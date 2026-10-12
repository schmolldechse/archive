namespace Archive.Core.Models.Archive;

public sealed record ArchiveCreationRequest(string OriginUrl, string Title, string? Description,
    IReadOnlyList<string>? Tags, Guid? CreatedByUserId = null);
