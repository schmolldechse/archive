using System.Text.Json;
using Archive.Core.Storage;
using Archive.Worker.Capture;

namespace Archive.Worker.Storage;

internal sealed class SnapshotObjectWriter(IArchiveObjectStore objectStore, Guid snapshotId,
    long maxStorageBytes, ILogger logger)
{
    private readonly List<string> _storedObjectKeys = [];

    public string ContentPrefix { get; } = $"snapshots/{snapshotId:N}";
    public long StorageBytes { get; private set; }
    public IReadOnlyList<string> StoredObjectKeys => _storedObjectKeys.ToArray();

    public string GetObjectKey(string relativePath) => $"{ContentPrefix}/{relativePath}";

    public void EnsureCapacity(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        if (byteCount > maxStorageBytes - StorageBytes)
            throw new CaptureDiscardedException("Das Speicherlimit wurde überschritten.");
    }

    public async Task WriteAsync(string path, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        EnsureCapacity(bytes.LongLength);
        var key = GetObjectKey(path);
        _storedObjectKeys.Add(key);
        await using var stream = new MemoryStream(bytes, writable: false);
        await objectStore.PutAsync(key, stream, contentType, cancellationToken);
        StorageBytes += bytes.LongLength;
    }

    public Task WriteManifestAsync<T>(T manifest, JsonSerializerOptions? options, CancellationToken cancellationToken) =>
        WriteAsync("manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest, options), "application/json", cancellationToken);

    public async Task RollbackAsync()
    {
        foreach (var key in _storedObjectKeys.AsEnumerable().Reverse().Distinct(StringComparer.Ordinal))
        {
            try { await objectStore.DeleteAsync(key, CancellationToken.None); }
            catch (Exception exception) { logger.LogWarning(exception, "Archive object {ObjectKey} could not be removed", key); }
        }
    }
}
