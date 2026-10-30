namespace Archive.Core.Storage.Manifests.Contracts.Shared;

/// <summary>Stable resource-role names persisted in snapshot manifests.</summary>
public static class SnapshotResourceRoles
{
    public const string RootDocument = "root-document";
    public const string NestedDocument = "nested-document";
    public const string Asset = "asset";

    /// <summary>Infers the conservative browser role used while upgrading older static snapshot generations.</summary>
    public static string Infer(string path, string contentType)
    {
        if (string.Equals(path, "index.html", StringComparison.OrdinalIgnoreCase))
            return RootDocument;
        if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            return NestedDocument;
        return Asset;
    }
}
