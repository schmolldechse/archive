namespace Archive.Worker.Capture;

public sealed record WebBotAuthOptions(
    bool Enabled,
    Uri? SignatureAgent,
    string? PrivateKeyPath,
    TimeSpan SignatureLifetime)
{
    public static WebBotAuthOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("Archive:WebBotAuth");
        var enabled = section.GetValue("Enabled", false);
        var agentValue = section["SignatureAgent"]?.Trim();
        var keyPath = section["PrivateKeyPath"]?.Trim();
        var lifetimeSeconds = section.GetValue("SignatureLifetimeSeconds", 60);

        if (lifetimeSeconds is < 10 or > 300)
            throw new InvalidOperationException(
                "Archive:WebBotAuth:SignatureLifetimeSeconds muss zwischen 10 und 300 liegen.");

        Uri? agent = null;
        if (!string.IsNullOrWhiteSpace(agentValue) &&
            (!Uri.TryCreate(agentValue, UriKind.Absolute, out agent) ||
             agent.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(agent.Query) ||
             !string.IsNullOrEmpty(agent.Fragment) || agent.AbsolutePath is not ("" or "/")))
        {
            throw new InvalidOperationException(
                "Archive:WebBotAuth:SignatureAgent muss ein HTTPS-Ursprung ohne Pfad, Query oder Fragment sein.");
        }

        if (enabled && agent is null)
            throw new InvalidOperationException(
                "Archive:WebBotAuth:SignatureAgent ist bei aktivierter Signierung erforderlich.");
        if (enabled && string.IsNullOrWhiteSpace(keyPath))
            throw new InvalidOperationException(
                "Archive:WebBotAuth:PrivateKeyPath ist bei aktivierter Signierung erforderlich.");

        return new WebBotAuthOptions(enabled, agent, keyPath, TimeSpan.FromSeconds(lifetimeSeconds));
    }
}
