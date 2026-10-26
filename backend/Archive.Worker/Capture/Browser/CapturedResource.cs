namespace Archive.Worker.Capture.Browser;

public sealed record CapturedResource(string Url, string ContentType, byte[] Body);

/// <summary>The final successful main navigation response retained as immutable capture provenance.</summary>
public sealed record CapturedMainDocument(string Url, string ContentType, int StatusCode, byte[] Body,
    IReadOnlyList<string> RedirectChain);
