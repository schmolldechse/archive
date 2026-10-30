using System.Text.Json;
using Archive.Core.Storage.Manifests;
using Archive.Core.Storage.Manifests.Models;

namespace Archive.Core.Storage.Manifests.Adapters;

/// <summary>Provides type-safe deserialization before a version adapter applies contract-specific semantics.</summary>
internal abstract class TypedSnapshotManifestAdapter<TContract>(int version) : SnapshotManifestAdapter(version)
    where TContract : class
{
    public sealed override ResolvedSnapshotManifest Resolve(JsonElement root, SnapshotManifestReadContext context)
    {
        var contract = root.Deserialize<TContract>() ??
            throw new InvalidDataException($"Snapshot manifest version {Version} could not be deserialized.");
        return Resolve(contract, context);
    }

    protected abstract ResolvedSnapshotManifest Resolve(TContract contract, SnapshotManifestReadContext context);
}
