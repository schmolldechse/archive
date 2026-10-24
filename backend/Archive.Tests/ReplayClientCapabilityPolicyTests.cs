using Archive.Api.Options;
using Archive.Api.Replay;

namespace Archive.Tests;

public sealed class ReplayClientCapabilityPolicyTests
{
    private readonly ReplayClientCapabilityPolicy _policy = new(new ReplayOptions
    {
        PublicOrigin = "http://127.0.0.1:5200",
        MinimumChromiumMajorVersion = 152,
        AllowedBrands = ["Chromium"]
    });

    [Theory]
    [InlineData("\"Chromium\";v=\"152\"")]
    [InlineData("\"Not_A Brand\";v=\"99\", \"Chromium\";v=\"156\"")]
    public void AllowsTestedChromiumHints(string header) => Assert.True(_policy.AllowsInteraction(header));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mozilla/5.0 Chrome/156")]
    [InlineData("\"Chromium\";v=\"151\"")]
    [InlineData("\"Google Chrome\";v=\"156\"")]
    [InlineData("\"Chromium\";v=\"156\", broken")]
    [InlineData("\"Chromium\";v=\"152\", \"Chromium\";v=\"153\"")]
    public void RejectsMissingOldUnknownOrMalformedHints(string? header) =>
        Assert.False(_policy.AllowsInteraction(header));
}
