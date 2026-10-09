using Archive.Api.Authentication;
using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Mapping;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Archive.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
[Description("External authentication providers and local user sessions.")]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
public sealed class AuthenticationController(
    IAuthenticationProviderCatalog providerCatalog,
    ArchiveAuthenticationOptions options,
    AuthenticationMapper mapper,
    ArchiveSessionReader sessions) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("providers")]
    [EndpointName("ListAuthenticationProviders")]
    [EndpointSummary("Lists authentication providers.")]
    [EndpointDescription("Returns the key and display name of each registered external authentication provider.")]
    [ProducesResponseType<IReadOnlyCollection<AuthenticationProvider>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyCollection<AuthenticationProvider>> GetProviders()
    {
        var providers = providerCatalog
            .GetAll()
            .Select(mapper.ToResponse)
            .ToArray();
        return Ok(providers);
    }

    [AllowAnonymous]
    [HttpGet("session")]
    [EndpointName("GetAuthenticationSession")]
    [EndpointSummary("Returns the current session.")]
    [EndpointDescription("Returns the authentication state and current user profile. Anonymous requests return an unauthenticated session with no user. An authenticated session with an invalid user identifier or a missing user returns HTTP 401.")]
    [ProducesResponseType<AuthenticationSession>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AuthenticationSession>> GetSession(CancellationToken cancellationToken) =>
        Ok(await sessions.ReadAsync(User, cancellationToken));

    [HttpPost("login/{provider}"), Consumes("application/x-www-form-urlencoded")]
    [EndpointName("BeginExternalLogin")]
    [EndpointSummary("Starts external authentication.")]
    [EndpointDescription("Redirects the browser to the selected authentication provider. The optional returnUrl form field specifies the local frontend path after authentication.")]
    [ProducesResponseType(302)]
    [ProducesResponseType<ProblemDetails>(400, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public IActionResult Login(string provider, [FromForm] LoginRequest request)
    {
        if (!ModelState.IsValid) return Error(400, "invalid_request", "The login form is invalid.");
        if (!providers.TryGet(provider, out var selected))
            return Error(404, "provider_unavailable", "This provider is unavailable.");
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = ArchiveAuthenticationOptions.LocalReturnUrl(request.ReturnUrl),
            IsPersistent = false
        }, selected.Scheme);
    }

    [HttpPost("logout"), EndpointName("Logout"), EndpointSummary("Ends the current session.")]
    [EndpointDescription("Removes the local session cookie and redirects the browser to the frontend homepage. The external provider session remains active.")]
    [ProducesResponseType(302)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(ArchiveAuthenticationExtension.SessionScheme);
        return Redirect(options.FrontendUrl("/"));
    }

    private ObjectResult Error(int status, string code, string detail) => Problem(detail, statusCode: status,
        extensions: new Dictionary<string, object?> { ["code"] = code, ["traceId"] = HttpContext.TraceIdentifier });
}
