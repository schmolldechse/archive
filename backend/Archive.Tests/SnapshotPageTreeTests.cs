using Archive.Core.Storage;

namespace Archive.Tests;

public sealed class SnapshotPageTreeTests
{
    [Fact]
    public void AcceptsValidBreadthFirstPageTree()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/", "Example"),
            Page("pages/a.html", "index.html", "https://example.com/a", "Section A"),
            Page("pages/b.html", "index.html", "https://example.com/b", null),
            Page("pages/c.html", "pages/a.html", "https://example.com/c", "Section C")
        ]);

        Assert.True(SnapshotManifestReader.TryGetPageTree(manifest, out var pageTree));
        Assert.Same(manifest.PageTree, pageTree);
    }

    [Fact]
    public void RejectsCycleInStoredPageTree()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/"),
            Page("pages/a.html", "pages/b.html", "https://example.com/a"),
            Page("pages/b.html", "pages/a.html", "https://example.com/b")
        ]);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsPageThatIsNotAnAuthorizedDocument()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/"),
            Page("pages/missing.html", "index.html", "https://example.com/missing")
        ], includeAllPageResources: false);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsOrphanedPage()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/"),
            Page("pages/a.html", "pages/missing.html", "https://example.com/a")
        ]);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsDuplicateDocumentResourcePath()
    {
        var manifest = Manifest([Page("index.html", null, "https://example.com/")],
            duplicateRootResource: true);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsDocumentResourceThatIsMissingFromTree()
    {
        var manifest = Manifest([Page("index.html", null, "https://example.com/")],
            extraDocumentResource: true);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsNonRootPageWithRootDocumentRole()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/"),
            Page("pages/a.html", "index.html", "https://example.com/a")
        ], secondRootResource: true);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void RejectsPageListThatIsNotBreadthFirst()
    {
        var manifest = Manifest([
            Page("index.html", null, "https://example.com/"),
            Page("pages/a.html", "index.html", "https://example.com/a"),
            Page("pages/c.html", "pages/a.html", "https://example.com/c"),
            Page("pages/b.html", "index.html", "https://example.com/b")
        ]);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Theory]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("two  spaces")]
    [InlineData("line\nbreak")]
    public void RejectsUnnormalizedTitle(string title)
    {
        var manifest = Manifest([Page("index.html", null, "https://example.com/", title)]);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    [Fact]
    public void LegacyManifestWithoutPageTreeRemainsReadableButHasNoTree()
    {
        var manifest = Manifest([Page("index.html", null, "https://example.com/")], withPageTree: false);

        Assert.False(SnapshotManifestReader.TryGetPageTree(manifest, out _));
    }

    private static SnapshotPageTreeEntry Page(string path, string? parentPath, string sourceUrl,
        string? title = null) => new()
    {
        Path = path,
        ParentPath = parentPath,
        SourceUrl = sourceUrl,
        Title = title
    };

    private static SnapshotManifest Manifest(IReadOnlyList<SnapshotPageTreeEntry> pages,
        bool includeAllPageResources = true, bool withPageTree = true, bool duplicateRootResource = false,
        bool extraDocumentResource = false, bool secondRootResource = false)
    {
        var resources = pages.Where((_, index) => includeAllPageResources || index == 0).Select((page, index) =>
            new SnapshotResource
            {
                Path = page.Path,
                SourceUrl = page.SourceUrl,
                ContentType = "text/html; charset=utf-8",
                ObjectKey = $"snapshots/example/{page.Path}",
                Sha256 = new string('0', 64),
                Role = index == 0 || secondRootResource && index == 1
                    ? SnapshotResourceRoles.RootDocument
                    : SnapshotResourceRoles.NestedDocument
            }).ToList();
        if (duplicateRootResource)
            resources.Add(resources[0]);
        if (extraDocumentResource)
            resources.Add(new SnapshotResource
            {
                Path = "pages/extra.html",
                SourceUrl = "https://example.com/extra",
                ContentType = "text/html; charset=utf-8",
                ObjectKey = "snapshots/example/pages/extra.html",
                Sha256 = new string('0', 64),
                Role = SnapshotResourceRoles.NestedDocument
            });
        return new SnapshotManifest
        {
            Version = SnapshotManifest.CurrentVersion,
            Provenance = new()
            {
                DerivedDisplayDocument = true,
                SourceObjectKey = "snapshots/example/source.html",
                SourceSha256 = new string('0', 64),
                IndexSha256 = new string('0', 64),
                SourceUrl = pages[0].SourceUrl,
                SourceMimeType = "text/html",
                CapturedAt = DateTimeOffset.UtcNow,
                DocumentState = "original-response"
            },
            Interaction = new()
            {
                Normalizer = "test",
                ContainsExecutableContent = false
            },
            Resources = resources,
            PageTree = withPageTree
                ? new SnapshotPageTree { RootPath = pages[0].Path, Pages = pages }
                : null
        };
    }
}
