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
        var subjectId = Guid.NewGuid();
        var token = service.Create(snapshotId, subjectId);

        Assert.True(service.TryValidate(snapshotId, token, out var protectedSubjectId));
        Assert.Equal(subjectId, protectedSubjectId);
        Assert.False(service.TryValidate(Guid.NewGuid(), token, out _));
        Assert.False(service.TryValidate(snapshotId, token + "invalid", out _));
    }

    private static SnapshotContentAccessTokenService CreateService() => new(
        new EphemeralDataProtectionProvider(),
        new SnapshotContentOptions
        {
            PublicOrigin = "http://127.0.0.1:5200",
            AllowedBrands = ["Chromium"]
        });
}
