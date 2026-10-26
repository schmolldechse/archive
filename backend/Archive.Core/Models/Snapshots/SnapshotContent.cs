namespace Archive.Core.Models.Snapshots;

/// <summary>The caller owns Content and must dispose it after serving the resource.</summary>
public sealed record SnapshotContent(Stream Content, string ContentType, bool IsOriginalSource,
    string ResourceRole);
