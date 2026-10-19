using System.Security.Cryptography;
using System.Text.Json;
using Archive.Core.Entities.Authentication;

namespace Archive.Core.Authorization;

/// <summary>Opaque access-state fingerprint, including identity to detect handle reassignment.</summary>
public static class UserAccessTag
{
    /// <summary>Returns a quoted strong validator without exposing the internal user identifier.</summary>
    public static string Create(ArchiveUser user)
    {
        var state = new
        {
            user.Id,
            user.Handle,
            user.DisplayName,
            user.Role,
            Permissions = PermissionCatalog.All.Select(definition => new
            {
                definition.Key,
                DefaultAllowed = definition.DefaultRoles.Contains(user.Role),
                Override = user.PermissionOverrides.FirstOrDefault(item => item.Permission == definition.Key)?.Effect,
                Allowed = PermissionEvaluator.IsAllowed(user, definition.Key)
            })
        };
        return $"\"{Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state)))}\"";
    }
}
