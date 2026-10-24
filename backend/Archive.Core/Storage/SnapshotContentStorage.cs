using Archive.Core.Models.Snapshots;

namespace Archive.Core.Storage;

/// <summary>Reads a stored snapshot resource after its caller has applied the appropriate visibility scope.</summary>
internal static class SnapshotContentStorage
{
    public static async Task<SnapshotContentResult> OpenSnapshotContentAsync(this IArchiveObjectStore objectStore,
        string contentPrefix, string? path, CancellationToken cancellationToken)
    {
        if (!TryNormalizePath(path, out var safePath))
            return new SnapshotContentResult.InvalidPath();

        var stored = await objectStore.GetAsync($"{contentPrefix}/{safePath}", cancellationToken);
        return stored is null
            ? new SnapshotContentResult.NotFound()
            : new SnapshotContentResult.Found(new(stored.Content, stored.ContentType,
                safePath.StartsWith("source.", StringComparison.OrdinalIgnoreCase),
                SnapshotReplayResourceRoles.Infer(safePath, stored.ContentType), null));
    }

    /// <summary>Normalizes a requested object path without allowing traversal or control characters.</summary>
    public static bool TryNormalizePath(string? path, out string safePath)
    {
        safePath = string.IsNullOrWhiteSpace(path) ? "index.html" : path.Replace('\\', '/').TrimStart('/');
        return !safePath.Contains("..", StringComparison.Ordinal) && !safePath.Any(char.IsControl);
    }
}
