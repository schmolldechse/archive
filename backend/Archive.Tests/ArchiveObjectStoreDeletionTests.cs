using System.Collections.Concurrent;
using Archive.Core.Storage;

namespace Archive.Tests;

public sealed class ArchiveObjectStoreDeletionTests
{
    [Fact]
    public async Task DeleteSnapshotObjectsRemovesUploadAndExactPrefixOnly()
    {
        var store = new MemoryObjectStore(
            "jobs/target/source.html",
            "snapshots/target/index.html",
            "snapshots/target/assets/site.css",
            "snapshots/target-neighbor/index.html");

        await store.DeleteSnapshotObjectsAsync("snapshots/target", "jobs/target/source.html");

        Assert.Equal(["snapshots/target-neighbor/index.html"], store.Keys);
    }

    [Fact]
    public async Task DeleteSnapshotObjectsIsIdempotentForMissingObjects()
    {
        var store = new MemoryObjectStore();

        await store.DeleteSnapshotObjectsAsync("snapshots/missing", "jobs/missing/source.html");
        await store.DeleteSnapshotObjectsAsync("snapshots/missing/", "jobs/missing/source.html");

        Assert.Empty(store.Keys);
    }

    [Fact]
    public async Task DeleteSnapshotObjectsPropagatesStorageFailure()
    {
        var store = new MemoryObjectStore("snapshots/target/index.html")
        {
            FailingKey = "snapshots/target/index.html"
        };

        await Assert.ThrowsAsync<IOException>(() =>
            store.DeleteSnapshotObjectsAsync("snapshots/target", null));

        Assert.Equal(["snapshots/target/index.html"], store.Keys);
    }

    private sealed class MemoryObjectStore(params string[] keys) : IArchiveObjectStore
    {
        private readonly ConcurrentDictionary<string, byte> _objects = new(
            keys.Select(key => new KeyValuePair<string, byte>(key, 0)), StringComparer.Ordinal);

        public string? FailingKey { get; init; }
        public string[] Keys => _objects.Keys.Order(StringComparer.Ordinal).ToArray();

        public Task PutAsync(string key, Stream content, string contentType,
            CancellationToken cancellationToken = default)
        {
            _objects[key] = 0;
            return Task.CompletedTask;
        }

        public Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<ArchiveObject?>(null);

        public Task<IReadOnlyList<ArchiveObjectInfo>> ListAsync(string prefix,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArchiveObjectInfo>>(
            _objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .Order(StringComparer.Ordinal)
                .Select(key => new ArchiveObjectInfo(key, 0))
                .ToArray());

        public Task CopyAsync(string sourceKey, string destinationKey,
            CancellationToken cancellationToken = default)
        {
            if (_objects.ContainsKey(sourceKey)) _objects[destinationKey] = 0;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.Equals(key, FailingKey, StringComparison.Ordinal))
                throw new IOException("Simulated object-storage failure.");
            _objects.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
