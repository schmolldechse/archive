using System.Text;
using Archive.Core.Storage;

namespace Archive.Tests;

public sealed class SnapshotManifestReaderTests
{
    private const string Prefix = "snapshots/11111111111111111111111111111111";
    private static readonly ISnapshotManifestReader Reader = new SnapshotManifestReader([
        new SnapshotManifestV1Adapter(),
        new SnapshotManifestV2Adapter(),
        new SnapshotManifestV3Adapter(),
        new SnapshotManifestV4Adapter()
    ]);

    [Theory]
    [InlineData("v1-url.json", 1, 3)]
    [InlineData("v2-import.json", 2, 2)]
    [InlineData("v3-replay.json", 3, 2)]
    [InlineData("v4-current.json", 4, 2)]
    public async Task ReadsEverySupportedManifestVersion(string fixture, int version, int resourceCount)
    {
        await using var stream = OpenFixture(fixture);

        var manifest = await Reader.ReadAsync(stream, Prefix, "https://fallback.test/",
            CancellationToken.None);

        Assert.Equal(version, manifest.Version);
        Assert.Equal(resourceCount, manifest.Resources.Count);
        Assert.Single(manifest.Resources, resource => resource.Path == "index.html" &&
            resource.Role == SnapshotResourceRoles.RootDocument);
    }

    [Fact]
    public async Task LegacyManifestInfersRolesAndRequiresCurrentNormalization()
    {
        await using var stream = OpenFixture("v1-url.json");

        var manifest = await Reader.ReadAsync(stream, Prefix, "https://fallback.test/",
            CancellationToken.None);

        Assert.True(manifest.Interaction.RequiresNormalization);
        Assert.True(manifest.Interaction.CanNormalize);
        Assert.Null(manifest.Resources.Single(resource => resource.Path == "index.html").Sha256);
        Assert.Equal(SnapshotResourceRoles.NestedDocument,
            manifest.Resources.Single(resource => resource.Path == "frames/child.html").Role);
    }

    [Fact]
    public async Task VersionThreeMapsReplayMetadataWithoutLosingItsHash()
    {
        await using var stream = OpenFixture("v3-replay.json");

        var manifest = await Reader.ReadAsync(stream, Prefix, null, CancellationToken.None);

        Assert.Equal("archive-snapshot-v1", manifest.Interaction.Normalizer);
        Assert.True(manifest.Interaction.RequiresNormalization);
        Assert.True(manifest.Interaction.CanNormalize);
        Assert.Equal(new string('b', 64), manifest.Resources.Single(resource => resource.Path == "index.html").Sha256);
    }

    [Theory]
    [InlineData("{\"version\":5}", "version 5 is not supported")]
    [InlineData("{\"resources\":[]}", "no integer version")]
    [InlineData("{\"version\":1,\"htmlObjectKey\":\"snapshots/other/index.html\",\"resources\":[]}",
        "outside its generation")]
    public async Task RejectsUnknownOrUnsafeManifestContracts(string json, string message)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => Reader.ReadAsync(
            stream, Prefix, null, CancellationToken.None));

        Assert.Contains(message, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RejectsLegacySourceObjectsInTheBrowserAllowlist()
    {
        var json = $$"""
            {
              "version": 2,
              "htmlObjectKey": "{{Prefix}}/index.html",
              "resources": [{
                "url": "https://example.test/source.mhtml",
                "contentType": "application/octet-stream",
                "objectKey": "{{Prefix}}/source.mhtml"
              }]
            }
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => Reader.ReadAsync(
            stream, Prefix, null, CancellationToken.None));

        Assert.Contains("reserved object", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsReservedObjectsInCurrentBrowserAllowlist()
    {
        var json = await ReadFixtureAsync("v4-current.json");
        json = json.Replace("assets/app.js", "screenshot.png", StringComparison.Ordinal);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => Reader.ReadAsync(
            stream, Prefix, null, CancellationToken.None));

        Assert.Contains("reserved object", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectsCurrentProvenanceOutsideTheActiveGeneration()
    {
        var json = await ReadFixtureAsync("v4-current.json");
        json = json.Replace($"{Prefix}/source.html", "snapshots/other/source.html", StringComparison.Ordinal);
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => Reader.ReadAsync(
            stream, Prefix, null, CancellationToken.None));

        Assert.Contains("invalid provenance", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RejectsDuplicateAdapterRegistrations()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => new SnapshotManifestReader([
            new SnapshotManifestV1Adapter(),
            new SnapshotManifestV1Adapter()
        ]));

        Assert.Contains("version 1", exception.Message, StringComparison.Ordinal);
    }

    private static Stream OpenFixture(string name)
    {
        var resourceName = $"Archive.Tests.Fixtures.SnapshotManifests.{name}";
        return typeof(SnapshotManifestReaderTests).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded fixture {resourceName} is missing.");
    }

    private static async Task<string> ReadFixtureAsync(string name)
    {
        await using var stream = OpenFixture(name);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
