namespace Archive.Core.Models.Archive;

/// <summary>Each call opens a new stream, owned and disposed by the repository.</summary>
public sealed record ArchiveUpload(string FileName, long Length, Func<Stream> OpenReadStream);
