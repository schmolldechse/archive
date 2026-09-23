namespace Archive.Worker.Import;

internal static class ImportStream
{
    public static async Task<byte[]> ReadAllBytesAsync(Stream source, long limit, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var chunk = new byte[64 * 1024];
        while (true)
        {
            var count = await source.ReadAsync(chunk, cancellationToken);
            if (count == 0)
                return buffer.ToArray();
            if (buffer.Length + count > limit)
                throw new InvalidDataException("The uploaded file exceeds the import byte limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
        }
    }
}
