using Archive.Core.Entities;
using Archive.Core.Models.Snapshots;

namespace Archive.Core.Storage;

/// <summary>Reads a stored snapshot resource after its caller has applied the appropriate visibility scope.</summary>
internal static class SnapshotContentStorage
{
    public static async Task<SnapshotContentResult> OpenSnapshotContentAsync(this IArchiveObjectStore objectStore,
        string contentPrefix, SourceType sourceType, string? path, CancellationToken cancellationToken)
    {
        var safePath = string.IsNullOrWhiteSpace(path) ? "index.html" : path.Replace('\\', '/').TrimStart('/');
        if (safePath.Contains("..", StringComparison.Ordinal)) return new SnapshotContentResult.InvalidPath();

        var stored = await objectStore.GetAsync($"{contentPrefix}/{safePath}", cancellationToken);
        return stored is null
            ? new SnapshotContentResult.NotFound()
            : new SnapshotContentResult.Found(new(stored.Content, stored.ContentType,
                safePath.StartsWith("source.", StringComparison.OrdinalIgnoreCase)));
    }
}
