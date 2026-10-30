using System.Text.Json;

namespace Archive.Core.Storage;

/// <summary>Dispatches persisted snapshot manifests to the adapter registered for their declared version.</summary>
internal sealed class SnapshotManifestReader : ISnapshotManifestReader
{
    private readonly IReadOnlyDictionary<int, SnapshotManifestAdapter> _adapters;

    public SnapshotManifestReader(IEnumerable<SnapshotManifestAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        var registered = new Dictionary<int, SnapshotManifestAdapter>();
        foreach (var adapter in adapters)
        {
            if (adapter is null)
                throw new ArgumentException("Snapshot manifest adapters cannot contain null entries.", nameof(adapters));
            if (!registered.TryAdd(adapter.Version, adapter))
                throw new InvalidOperationException(
                    $"More than one snapshot manifest adapter is registered for version {adapter.Version}.");
        }
        if (registered.Count == 0)
            throw new InvalidOperationException("At least one snapshot manifest adapter must be registered.");
        _adapters = registered;
    }

    public async Task<ResolvedSnapshotManifest> ReadAsync(Stream content, string contentPrefix,
        string? fallbackSourceUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentPrefix);

        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("version", out var versionElement) ||
            !versionElement.TryGetInt32(out var version))
            throw new InvalidDataException("The snapshot manifest has no integer version.");
        if (!_adapters.TryGetValue(version, out var adapter))
            throw new InvalidDataException($"Snapshot manifest version {version} is not supported.");

        var prefix = contentPrefix.TrimEnd('/');
        if (prefix.Length == 0)
            throw new ArgumentException("The snapshot content prefix must identify a generation.", nameof(contentPrefix));

        var context = new SnapshotManifestReadContext(prefix, fallbackSourceUrl);
        var resolved = adapter.Resolve(root, context);
        SnapshotManifestValidation.Validate(resolved, context.ContentPrefix);
        return resolved;
    }
}
