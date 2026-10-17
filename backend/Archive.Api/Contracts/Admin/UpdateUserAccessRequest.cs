using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Authorization;
using Archive.Core.Entities.Authentication.Permissible;

namespace Archive.Api.Contracts.Admin;

/// <summary>Atomic partial update. Null role and a null/empty override list leave those fields unchanged.</summary>
[Description("Atomic partial access update. Unspecified fields and unlisted overrides remain unchanged.")]
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateUserAccessRequest : IValidatableObject
{
    [JsonPropertyName("role")]
    [Description("New role, or null/omitted to keep the current role.")]
    public ArchiveRole? Role { get; init; }

    [JsonPropertyName("permissionOverrides")]
    [Description("Explicit changes only. Unlisted permission overrides remain unchanged.")]
    public IReadOnlyList<PermissionOverrideRequest>? PermissionOverrides { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role is null && PermissionOverrides is not { Count: > 0 })
            yield return new("Provide a role or at least one permission change.", [nameof(Role), nameof(PermissionOverrides)]);
        if (Role.HasValue && !Enum.IsDefined(Role.Value))
            yield return new("Unknown role.", [nameof(Role)]);
        if (PermissionOverrides is null) yield break;
        if (PermissionOverrides.Count > PermissionCatalog.All.Length)
            yield return new("Too many permission changes.", [nameof(PermissionOverrides)]);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in PermissionOverrides)
        {
            if (change is null || !PermissionCatalog.TryGet(change.Permission, out _) || !keys.Add(change.Permission) ||
                change.Effect.HasValue && !Enum.IsDefined(change.Effect.Value))
                yield return new("Each change must name a known, distinct permission and a valid effect.", [nameof(PermissionOverrides)]);
        }
    }
}
