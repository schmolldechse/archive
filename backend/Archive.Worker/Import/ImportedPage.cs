namespace Archive.Worker.Import;

public sealed record ImportedResource(
    string Url,
    string ContentType,
    byte[] Body,
    string? TextEncoding = null,
    IReadOnlyList<string>? Aliases = null);

public sealed record ImportedPage(
    string Html,
    string? BaseUrl,
    IReadOnlyList<ImportedResource> Resources,
    IReadOnlyList<ImportedPage> Frames,
    string? FrameName = null);
