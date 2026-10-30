using Archive.Core.Storage.Manifests;
using Archive.Core.Storage.Manifests.Contracts.Legacy;
using Archive.Core.Storage.Manifests.Contracts.Shared;
using Archive.Core.Storage.Manifests.Models;

namespace Archive.Core.Storage.Manifests.Adapters;

/// <summary>Maps the shared V1/V2 object layout while preserving their distinct source-URL semantics.</summary>
internal abstract class LegacySnapshotManifestAdapter<TContract>(int version)
    : TypedSnapshotManifestAdapter<TContract>(version)
    where TContract : class, ILegacySnapshotManifestContract
{
    protected sealed override ResolvedSnapshotManifest Resolve(TContract contract,
        SnapshotManifestReadContext context)
    {
        if (contract.Version != Version || string.IsNullOrWhiteSpace(contract.HtmlObjectKey) ||
            contract.Resources is null || contract.Resources.Any(resource => resource is null))
            throw IncompleteContract();

        var sourceUrl = ResolveSourceUrl(contract, context);
        var resources = new List<ResolvedSnapshotResource>(contract.Resources.Count + 1)
        {
            new("index.html", sourceUrl, "text/html; charset=utf-8", contract.HtmlObjectKey, null,
                SnapshotResourceRoles.RootDocument)
        };

        foreach (var resource in contract.Resources)
        {
            if (string.IsNullOrWhiteSpace(resource.ObjectKey) ||
                !SnapshotManifestValidation.TryGetRelativePath(
                    context.ContentPrefix, resource.ObjectKey, out var path) ||
                string.IsNullOrWhiteSpace(resource.ContentType))
                throw new InvalidDataException(
                    $"Snapshot manifest version {Version} contains an invalid resource.");
            if (SnapshotManifestValidation.IsReserved(path))
                throw new InvalidDataException(
                    $"Snapshot manifest version {Version} authorizes a reserved object.");

            resources.Add(new(path, resource.Url, resource.ContentType, resource.ObjectKey, null,
                SnapshotResourceRoles.Infer(path, resource.ContentType)));
        }

        return new(Version, new(sourceUrl),
            new(null, "Legacy manifest requires current normalization.", true, true), resources);
    }

    protected abstract string? ResolveSourceUrl(TContract contract,
        SnapshotManifestReadContext context);
}
