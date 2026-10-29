namespace Archive.Core.Storage;

public sealed record ArchiveObject(Stream Content, string ContentType, long? Length);

/// <summary>Content metadata read without opening an object's response body.</summary>
public sealed record ArchiveObjectMetadata(string ContentType, long Length);

/// <summary>Metadata for an object stored below an archive prefix.</summary>
public sealed record ArchiveObjectInfo(string Key, long Length);

public interface IArchiveObjectStore
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<ArchiveObjectMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Reads one inclusive byte interval without downloading the preceding object bytes.</summary>
    Task<ArchiveObject?> GetRangeAsync(string key, long offset, long length,
        CancellationToken cancellationToken = default);
    /// <summary>Lists every object whose key starts with the supplied prefix.</summary>
    Task<IReadOnlyList<ArchiveObjectInfo>> ListAsync(string prefix, CancellationToken cancellationToken = default);
    /// <summary>Copies an object without changing its stored content or metadata.</summary>
    Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
