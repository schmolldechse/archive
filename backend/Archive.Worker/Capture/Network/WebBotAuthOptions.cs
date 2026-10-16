using System.ComponentModel.DataAnnotations;

namespace Archive.Worker.Capture.Network;

public sealed class WebBotAuthOptions : IValidatableObject
{
    public const string SectionName = "Archive:WebBotAuth";

    public bool Enabled { get; set; }
    public string? SignatureAgent { get; set; }
    public string? PrivateKeyPath { get; set; }

    [Range(10, 300)]
    public int SignatureLifetimeSeconds { get; set; } = 60;

    public TimeSpan SignatureLifetime => TimeSpan.FromSeconds(SignatureLifetimeSeconds);
    public Uri? SignatureAgentUri => Uri.TryCreate(SignatureAgent, UriKind.Absolute, out var uri) ? uri : null;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(SignatureAgent) &&
            (!Uri.TryCreate(SignatureAgent, UriKind.Absolute, out var agent) || agent.Scheme != Uri.UriSchemeHttps ||
             agent.UserInfo.Length != 0 || agent.Query.Length != 0 || agent.Fragment.Length != 0 || agent.AbsolutePath != "/"))
            yield return new ValidationResult("SignatureAgent must be an HTTPS origin without credentials, path, query or fragment.", [nameof(SignatureAgent)]);
        if (Enabled && string.IsNullOrWhiteSpace(SignatureAgent))
            yield return new ValidationResult("SignatureAgent is required when Web Bot Auth is enabled.", [nameof(SignatureAgent)]);
        if (Enabled && string.IsNullOrWhiteSpace(PrivateKeyPath))
            yield return new ValidationResult("PrivateKeyPath is required when Web Bot Auth is enabled.", [nameof(PrivateKeyPath)]);
    }
}
