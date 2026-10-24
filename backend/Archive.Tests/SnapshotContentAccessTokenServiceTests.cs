using Archive.Api.Options;
using Archive.Api.SnapshotContent;
using Microsoft.AspNetCore.DataProtection;

namespace Archive.Tests;

public sealed class SnapshotContentAccessTokenServiceTests
{
    [Fact]
    public void CapabilityIsBoundToOneSnapshot()
    {
        var service = CreateService();
        var snapshotId = Guid.NewGuid();
        var token = service.Create(snapshotId);

        Assert.True(service.IsValid(snapshotId, token));
        Assert.False(service.IsValid(Guid.NewGuid(), token));
        Assert.False(service.IsValid(snapshotId, token + "invalid"));
    }

    private static SnapshotContentAccessTokenService CreateService() => new(
        new EphemeralDataProtectionProvider(),
        new SnapshotContentOptions
        {
            PublicOrigin = "http://127.0.0.1:5200",
            AllowedBrands = ["Chromium"]
        });
}
