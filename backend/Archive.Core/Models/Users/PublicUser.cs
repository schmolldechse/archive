using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Core.Models.Users;

/// <summary>Publicly shareable account information.</summary>
public sealed record PublicUser(string Handle, string DisplayName, bool HasProfileImage, ArchiveRole Role);
