using System.ComponentModel;
using Archive.Api.Authentication;
using Archive.Api.Authorization.Attributes;
using Archive.Api.Contracts.Admin;
using Archive.Api.Mapping;
using Archive.Core.Authorization;
using Archive.Core.Models.Users;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Archive.Api.Controllers.Admin;

/// <summary>Registered-user administration with per-operation permissions and conditional writes.</summary>
[ApiController]
[Route("api/admin/users")]
[Tags("Admin Users")]
[Description("Registered-user role and personal permission administration.")]
[RequirePermission(ArchivePermissions.AccessAdminPortal)]
[RequirePermission(ArchivePermissions.AccessAdminUsers)]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
public sealed class UsersController(IUserRepository users, UserAccessMapper mapper,
    IAuthorizationService authorization, ArchiveAuthenticationOptions options) : ControllerBase
{
    /// <summary>Searches registered accounts by handle, display name, and role.</summary>
    [HttpGet]
    [EndpointName("ListUsers")]
    [EndpointSummary("Lists registered users.")]
    [EndpointDescription("Requires admin.access and admin.users.access. Searches handle, display name, and role, combines the optional role filter, and returns registered profiles ordered by handle.")]
    [ProducesResponseType<UserListResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<UserListResponse>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken)
    {
        var page = await users.SearchAsync(query.Q, query.Role, query.Page, query.PageSize, cancellationToken);
        return Ok(mapper.ToResponse(page));
    }

    /// <summary>Reads access settings and a strong ETag for a registered account's current handle.</summary>
    [HttpGet("{handle}/access")]
    [EndpointName("GetUserAccess")]
    [EndpointSummary("Returns saved user access settings.")]
    [EndpointDescription("Requires admin.access and admin.users.access. Returns the saved role and every supported permission with its role default, personal override, and effective result. Includes a strong ETag for conditional updates.")]
    [ProducesResponseType<UserAccessResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UserAccessResponse>> Get(string handle, CancellationToken cancellationToken)
    {
        var user = await users.GetAccessAsync(handle, cancellationToken);
        if (user is null) return AccessProblem(404, "User unavailable", "This registered user could not be found.", "user_not_found");
        Response.Headers.ETag = UserAccessTag.Create(user);
        return Ok(mapper.ToResponse(user));
    }

    /// <summary>Atomically changes requested fields. Requires their respective write permissions.</summary>
    [HttpPatch("{handle}/access")]
    [Consumes("application/json")]
    [EndpointName("UpdateUserAccess")]
    [EndpointSummary("Updates a user's role and personal permission overrides.")]
    [EndpointDescription("Requires admin.access and admin.users.access. Role changes require users.assign_roles; override changes require users.assign_permissions. Send exactly one strong If-Match ETag from GET on this resource and the configured frontend Origin. Self changes and removal of the last fully authorized Admin are rejected. No partial updates are saved.")]
    [ProducesResponseType<UserAccessResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status412PreconditionFailed, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status428PreconditionRequired, "application/problem+json")]
    public async Task<ActionResult<UserAccessResponse>> Update(string handle, [FromBody] UpdateUserAccessRequest request,
        [FromHeader(Name = "If-Match"), Description("Exactly one strong ETag returned by GET on this access resource. Required for saving.")] string? ifMatch, CancellationToken cancellationToken)
    {
        // Browser-set Origin is mandatory for this cookie-authenticated JSON mutation.
        if (!string.Equals(Request.Headers.Origin.ToString(), options.FrontendOrigin.TrimEnd('/'), StringComparison.Ordinal))
            return AccessProblem(403, "Request origin not allowed", "Use the configured frontend origin.", "request_origin_not_allowed");
        var match = Request.Headers.IfMatch;
        if (match.Count == 0) return AccessProblem(428, "Precondition required", "Load current access settings before saving.", "if_match_required");
        if (match.Count != 1 || !EntityTagHeaderValue.TryParse(ifMatch, out var tag) || tag.IsWeak || tag.Tag.Value == "*")
            return AccessProblem(400, "Invalid precondition", "Provide exactly one strong ETag in If-Match.", "invalid_if_match");

        // Attribute policies protect the resource; request-dependent rights follow model binding.
        if (request.Role.HasValue && !(await authorization.AuthorizeAsync(User, HttpContext, ArchivePermissions.AssignUserRoles)).Succeeded ||
            request.PermissionOverrides is { Count: > 0 } && !(await authorization.AuthorizeAsync(User, HttpContext, ArchivePermissions.AssignUserPermissions)).Succeeded)
            return Forbid();

        var update = new UserAccessUpdate(request.Role, request.PermissionOverrides?.ToDictionary(item => item.Permission, item => item.Effect)
            ?? new Dictionary<string, Core.Entities.Authentication.Permissible.ArchivePermissionEffect?>());
        var result = await users.UpdateAccessAsync(User.ReadUserId(), handle, tag.ToString(), update, cancellationToken);
        switch (result)
        {
            case UserAccessUpdateResult.Updated saved:
                Response.Headers.ETag = UserAccessTag.Create(saved.User);
                return Ok(mapper.ToResponse(saved.User));
            case UserAccessUpdateResult.NotFound:
                return AccessProblem(404, "User unavailable", "This registered user could not be found.", "user_not_found");
            case UserAccessUpdateResult.Forbidden: return Forbid();
            case UserAccessUpdateResult.Stale:
                return AccessProblem(412, "Access settings changed", "Reload access before saving again.", "access_changed");
            case UserAccessUpdateResult.SelfChange:
                return AccessProblem(409, "Own access is read-only", "Ask another administrator to change your access.", "self_access_change");
            case UserAccessUpdateResult.LastAdministrator:
                return AccessProblem(409, "An administrator must remain", "Keep at least one Admin with portal, user, role, and permission management access.", "last_administrator");
            default: throw new InvalidOperationException("Unknown access update result.");
        }
    }

    private ObjectResult AccessProblem(int status, string title, string detail, string code) =>
        Problem(statusCode: status, title: title, detail: detail, extensions: new Dictionary<string, object?> { ["code"] = code });
}
