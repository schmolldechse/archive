namespace Archive.Core.Models.Authentication;

public sealed record ExternalIdentityDescriptor(
    string ProviderKey,
    string ProviderDisplayName,
    string ExternalSubject,
    string? Email,
    string DisplayName,
    byte[]? ProfilePicture);