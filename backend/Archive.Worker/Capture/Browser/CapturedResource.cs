namespace Archive.Worker.Capture.Browser;

public sealed record CapturedResource(string Url, string ContentType, byte[] Body);
