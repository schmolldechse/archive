namespace Archive.Worker.Capture;

public sealed record CrawlPolicyOptions(
    bool Enabled,
    string UserAgentToken,
    TimeSpan MinimumHostDelay,
    TimeSpan RequestTimeout,
    int MaxRobotsBytes)
{
    public static CrawlPolicyOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive:CrawlPolicy");
        var token = (section["UserAgentToken"] ?? "VoldechseArchiveBot").Trim();
        var delaySeconds = section.GetValue("MinimumHostDelaySeconds", 2d);
        var timeoutSeconds = section.GetValue("RequestTimeoutSeconds", 10);
        var maxBytes = section.GetValue("MaxRobotsBytes", 1_000_000);

        if (token.Length is < 1 or > 128 || token.Any(character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')))
            throw new InvalidOperationException(
                "Archive:CrawlPolicy:UserAgentToken darf nur ASCII-Buchstaben, Ziffern, '-' und '_' enthalten.");
        if (delaySeconds is < 0 or > 300)
            throw new InvalidOperationException(
                "Archive:CrawlPolicy:MinimumHostDelaySeconds muss zwischen 0 und 300 liegen.");
        if (timeoutSeconds is < 1 or > 60)
            throw new InvalidOperationException(
                "Archive:CrawlPolicy:RequestTimeoutSeconds muss zwischen 1 und 60 liegen.");
        if (maxBytes is < 1024 or > 5_000_000)
            throw new InvalidOperationException(
                "Archive:CrawlPolicy:MaxRobotsBytes muss zwischen 1024 und 5000000 liegen.");

        return new CrawlPolicyOptions(section.GetValue("Enabled", true), token,
            TimeSpan.FromSeconds(delaySeconds), TimeSpan.FromSeconds(timeoutSeconds), maxBytes);
    }
}
