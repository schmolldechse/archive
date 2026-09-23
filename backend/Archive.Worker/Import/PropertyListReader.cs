using System.Buffers.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Archive.Worker.Import;

internal static class PropertyListReader
{
    public static Dictionary<string, object?> Read(byte[] bytes, ImportLimits limits)
    {
        if (bytes.AsSpan().StartsWith("bplist00"u8))
            return new BinaryReader(bytes, limits).ReadRoot();
        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = limits.MaxBytes * 2
            });
            var root = XDocument.Load(reader).Root;
            if (root?.Name.LocalName != "plist" || root.Elements().SingleOrDefault() is not { } value)
                throw new InvalidDataException("The XML property list has no root value.");
            return ReadXmlValue(value, 0, limits) as Dictionary<string, object?>
                ?? throw new InvalidDataException("The Webarchive root must be a dictionary.");
        }
        catch (Exception exception) when (exception is XmlException or FormatException or InvalidOperationException or OverflowException)
        {
            throw new InvalidDataException("The XML property list is invalid.", exception);
        }
    }

    private static object? ReadXmlValue(XElement element, int depth, ImportLimits limits)
    {
        if (depth > limits.MaxDepth)
            throw new InvalidDataException("The property list is too deeply nested.");
        switch (element.Name.LocalName)
        {
            case "dict":
            {
                var children = element.Elements().ToArray();
                if (children.Length % 2 != 0 || children.Length > limits.MaxObjects * 2)
                    throw new InvalidDataException("The property list dictionary is invalid.");
                var result = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var i = 0; i < children.Length; i += 2)
                {
                    if (children[i].Name.LocalName != "key" || !result.TryAdd(children[i].Value, ReadXmlValue(children[i + 1], depth + 1, limits)))
                        throw new InvalidDataException("The property list has an invalid or duplicate key.");
                }
                return result;
            }
            case "array":
            {
                var elements = element.Elements().Take(limits.MaxObjects + 1).ToArray();
                if (elements.Length > limits.MaxObjects)
                    throw new InvalidDataException("The property list has too many array elements.");
                return elements.Select(x => ReadXmlValue(x, depth + 1, limits)).ToList();
            }
            case "string": return element.Value;
            case "data": return Convert.FromBase64String(element.Value);
            case "integer": return long.Parse(element.Value, System.Globalization.CultureInfo.InvariantCulture);
            case "true": return true;
            case "false": return false;
            case "real": return double.Parse(element.Value, System.Globalization.CultureInfo.InvariantCulture);
            case "date": return element.Value;
            default: throw new InvalidDataException($"Unsupported property list element '{element.Name.LocalName}'.");
        }
    }

    private sealed class BinaryReader
    {
        private readonly byte[] _bytes;
        private readonly ImportLimits _limits;
        private readonly int[] _offsets;
        private readonly int _objectRefSize;
        private readonly int _dataEnd;
        private readonly Dictionary<int, object?> _cache = [];
        private readonly HashSet<int> _active = [];
        private readonly int _rootIndex;

        public BinaryReader(byte[] bytes, ImportLimits limits)
        {
            _bytes = bytes;
            _limits = limits;
            if (bytes.Length < 40)
                throw new InvalidDataException("The binary property list is truncated.");
            var trailer = bytes.AsSpan(bytes.Length - 32);
            var offsetSize = trailer[6];
            _objectRefSize = trailer[7];
            var objectCount = BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(8, 8));
            var root = BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(16, 8));
            var tableOffset = BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(24, 8));
            _dataEnd = bytes.Length - 32;
            if (offsetSize is not (1 or 2 or 4 or 8) || _objectRefSize is not (1 or 2 or 4 or 8) ||
                objectCount == 0 || objectCount > (ulong)limits.MaxObjects || root >= objectCount ||
                tableOffset < 8 || tableOffset > (ulong)_dataEnd ||
                objectCount > ((ulong)_dataEnd - tableOffset) / offsetSize)
                throw new InvalidDataException("The binary property list trailer is invalid.");
            _rootIndex = (int)root;
            _dataEnd = (int)tableOffset;
            _offsets = new int[(int)objectCount];
            for (var i = 0; i < _offsets.Length; i++)
            {
                var offset = ReadUnsigned((int)tableOffset + i * offsetSize, offsetSize, bytes.Length - 32);
                if (offset < 8 || offset >= (ulong)_dataEnd)
                    throw new InvalidDataException("A property list object offset is outside the data section.");
                _offsets[i] = (int)offset;
            }
        }

        public Dictionary<string, object?> ReadRoot() => ReadObject(_rootIndex, 0) as Dictionary<string, object?>
            ?? throw new InvalidDataException("The Webarchive root must be a dictionary.");

        private object? ReadObject(int index, int depth)
        {
            if (index < 0 || index >= _offsets.Length || depth > _limits.MaxDepth || !_active.Add(index))
                throw new InvalidDataException("The property list has an invalid or cyclic object reference.");
            if (_cache.TryGetValue(index, out var cached))
            {
                _active.Remove(index);
                return cached;
            }
            try
            {
                var position = _offsets[index];
                var marker = _bytes[position++];
                var kind = marker >> 4;
                var info = marker & 15;
                object? result;
                switch (kind)
                {
                    case 0:
                        result = info switch { 0 => null, 8 => false, 9 => true, _ => throw new InvalidDataException("Unsupported property list simple value.") };
                        break;
                    case 1:
                        result = checked((long)ReadUnsigned(position, 1 << info, _dataEnd));
                        break;
                    case 2:
                        result = (1 << info) switch
                        {
                            4 => BitConverter.Int32BitsToSingle((int)ReadUnsigned(position, 4, _dataEnd)),
                            8 => BitConverter.Int64BitsToDouble((long)ReadUnsigned(position, 8, _dataEnd)),
                            _ => throw new InvalidDataException("Unsupported property list real value.")
                        };
                        break;
                    case 3:
                        result = BitConverter.Int64BitsToDouble((long)ReadUnsigned(position, 8, _dataEnd));
                        break;
                    case 4:
                    case 5:
                    case 6:
                    {
                        var length = ReadLength(info, ref position);
                        var byteLength = kind == 6 ? checked(length * 2) : length;
                        EnsureRange(position, byteLength, _dataEnd);
                        var span = _bytes.AsSpan(position, byteLength);
                        result = kind switch
                        {
                            4 => span.ToArray(),
                            5 => Encoding.ASCII.GetString(span),
                            _ => Encoding.BigEndianUnicode.GetString(span)
                        };
                        break;
                    }
                    case 8:
                        result = checked((long)ReadUnsigned(position, info + 1, _dataEnd));
                        break;
                    case 10:
                    {
                        var count = ReadLength(info, ref position);
                        if (count > _limits.MaxObjects)
                            throw new InvalidDataException("The property list array exceeds the object limit.");
                        EnsureRange(position, checked(count * _objectRefSize), _dataEnd);
                        var values = new List<object?>(count);
                        for (var i = 0; i < count; i++)
                            values.Add(ReadObject(ReadRef(position + i * _objectRefSize), depth + 1));
                        result = values;
                        break;
                    }
                    case 13:
                    {
                        var count = ReadLength(info, ref position);
                        if (count > _limits.MaxObjects)
                            throw new InvalidDataException("The property list dictionary exceeds the object limit.");
                        EnsureRange(position, checked(count * _objectRefSize * 2), _dataEnd);
                        var values = new Dictionary<string, object?>(count, StringComparer.Ordinal);
                        for (var i = 0; i < count; i++)
                        {
                            var key = ReadObject(ReadRef(position + i * _objectRefSize), depth + 1) as string
                                ?? throw new InvalidDataException("A property list dictionary key is not a string.");
                            var value = ReadObject(ReadRef(position + (count + i) * _objectRefSize), depth + 1);
                            if (!values.TryAdd(key, value))
                                throw new InvalidDataException("The property list has a duplicate key.");
                        }
                        result = values;
                        break;
                    }
                    default: throw new InvalidDataException($"Unsupported property list object type {kind}.");
                }
                _cache[index] = result;
                return result;
            }
            catch (OverflowException exception)
            {
                throw new InvalidDataException("A property list value exceeds the supported size.", exception);
            }
            finally
            {
                _active.Remove(index);
            }
        }

        private int ReadLength(int info, ref int position)
        {
            if (info != 15)
                return info;
            EnsureRange(position, 1, _dataEnd);
            var marker = _bytes[position++];
            if ((marker >> 4) != 1 || (1 << (marker & 15)) > 8)
                throw new InvalidDataException("The property list length is invalid.");
            var width = 1 << (marker & 15);
            var length = ReadUnsigned(position, width, _dataEnd);
            position += width;
            if (length > (ulong)_limits.MaxBytes || length > int.MaxValue)
                throw new InvalidDataException("A property list value exceeds the import limit.");
            return (int)length;
        }

        private int ReadRef(int position)
        {
            var value = ReadUnsigned(position, _objectRefSize, _dataEnd);
            if (value >= (ulong)_offsets.Length)
                throw new InvalidDataException("A property list object reference is invalid.");
            return (int)value;
        }

        private ulong ReadUnsigned(int position, int width, int end)
        {
            if (width is < 1 or > 8)
                throw new InvalidDataException("Unsupported property list integer width.");
            EnsureRange(position, width, end);
            ulong value = 0;
            for (var i = 0; i < width; i++)
                value = (value << 8) | _bytes[position + i];
            return value;
        }

        private static void EnsureRange(int position, int length, int end)
        {
            if (position < 0 || length < 0 || position > end || length > end - position)
                throw new InvalidDataException("A property list object exceeds the file boundary.");
        }
    }
}
