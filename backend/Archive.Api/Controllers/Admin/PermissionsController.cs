using Archive.Api.Authentication;
using Archive.Api.Authorization;
using Archive.Api.Contracts.Admin;
using Archive.Api.Mapping;
using System.ComponentModel;
using Archive.Core.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers.Admin;

/// <summary>Publishes the administration permission catalog to authorized readers.</summary>
[ApiController]
[Route("api/admin/permissions")]
[Tags("Admin Permissions")]
[Description("Supported permissions available to user administration.")]
[Authorize(Policy = ArchiveAuthorizationPolicies.UserAccessRead)]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class PermissionsController(UserAccessMapper mapper) : ControllerBase
{
    /// <summary>Lists supported permission names and descriptions, without role defaults.</summary>
    [HttpGet]
    [EndpointName("ListPermissions")]
    [EndpointSummary("Lists the permission catalog.")]
    [EndpointDescription("Requires admin.access and admin.users.access. Returns supported keys with English names and descriptions in stable catalog order. Internal role defaults are not published.")]
    [ProducesResponseType<PermissionCatalogResponse>(StatusCodes.Status200OK, "application/json")]
    public ActionResult<PermissionCatalogResponse> List() => Ok(new PermissionCatalogResponse
    {
        Items = PermissionCatalog.All.Select(mapper.ToResponse).ToArray()
    });
}
