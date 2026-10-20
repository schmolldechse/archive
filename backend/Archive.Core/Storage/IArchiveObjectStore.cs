namespace Archive.Core.Storage;

public sealed record ArchiveObject(Stream Content, string ContentType, long? Length);

public interface IArchiveObjectStore
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
    Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default);
    /// <summary>Copies an object without changing its stored content or metadata.</summary>
    Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
