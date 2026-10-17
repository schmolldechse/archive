using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Core.Models.Users;

/// <summary>Explicit changes only; a null override removes the stored personal exception.</summary>
public sealed record UserAccessUpdate(ArchiveRole? Role, IReadOnlyDictionary<string, ArchivePermissionEffect?> PermissionOverrides);
