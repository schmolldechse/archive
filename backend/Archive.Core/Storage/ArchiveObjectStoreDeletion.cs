namespace Archive.Core.Storage;

/// <summary>Exact, idempotent deletion helpers for archive object prefixes.</summary>
public static class ArchiveObjectStoreDeletion
{
    /// <summary>Deletes a temporary upload and every object below the snapshot's content prefix.</summary>
    public static async Task DeleteSnapshotObjectsAsync(this IArchiveObjectStore objectStore,
        string contentPrefix, string? uploadedObjectKey, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objectStore);
        if (uploadedObjectKey is not null)
            await objectStore.DeleteAsync(uploadedObjectKey, cancellationToken);
        await objectStore.DeletePrefixAsync(contentPrefix, cancellationToken);
    }

    /// <summary>Deletes and verifies every object strictly below the supplied prefix.</summary>
    public static async Task DeletePrefixAsync(this IArchiveObjectStore objectStore, string prefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objectStore);
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("An archive object prefix is required.", nameof(prefix));

        var normalizedPrefix = prefix.TrimEnd('/') + "/";
        var objects = await objectStore.ListAsync(normalizedPrefix, cancellationToken);
        foreach (var item in objects)
        {
            if (!item.Key.StartsWith(normalizedPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Object storage returned a key outside the requested prefix.");
            await objectStore.DeleteAsync(item.Key, cancellationToken);
        }

        if ((await objectStore.ListAsync(normalizedPrefix, cancellationToken)).Count > 0)
            throw new IOException("The archive object prefix is not empty after deletion.");
    }
}
