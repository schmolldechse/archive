using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Core.Models.Users;

/// <summary>Registered-user projection that avoids loading image bytes and external identities.</summary>
public sealed record UserListEntry(string Handle, string DisplayName, bool HasProfileImage, ArchiveRole Role);
