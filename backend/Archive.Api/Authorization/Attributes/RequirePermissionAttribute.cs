using Microsoft.AspNetCore.Authorization;

namespace Archive.Api.Authorization.Attributes;

/// <summary>Requires the registered policy for a permission. Multiple attributes combine with AND.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>Selects an exact permission key from the Core catalog.</summary>
    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);
        Policy = permission;
    }
}
