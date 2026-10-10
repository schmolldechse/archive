using Archive.Api.Authentication;
using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Exceptions;
using Archive.Api.Mapping;
using Archive.Core.Repositories.UserRepository;
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
    IUserRepository userRepository) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("providers")]
    [EndpointName("ListAuthenticationProviders")]
    [EndpointSummary("Lists authentication providers.")]
    [EndpointDescription("Returns the key and display name of each registered external authentication provider.")]
    [ProducesResponseType<IReadOnlyCollection<AuthenticationProvider>>(StatusCodes.Status200OK, "application/json")]
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
    [ProducesResponseType<AuthenticationSession>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<ActionResult<AuthenticationSession>> GetSession(CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Ok(new AuthenticationSession { IsAuthenticated = false });

        var account = await userRepository.GetByIdAsync(User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();

        return Ok(new AuthenticationSession
        {
            IsAuthenticated = true,
            User = mapper.ToResponse(account, account.GetProfileImageUrl(options.PublicApiOrigin))
        });
    }

    [HttpPost("login/{provider}")]
    [Consumes("application/x-www-form-urlencoded")]
    [EndpointName("BeginExternalLogin")]
    [EndpointSummary("Starts external authentication.")]
    [EndpointDescription("Starts a browser navigation to the selected authentication provider. The optional returnUrl query parameter specifies a local frontend path after authentication; its default is /account. A new profile is redirected to /account/setup. External authentication must be completed in the browser before testing session-protected endpoints.")]
    [ProducesResponseType(typeof(void), StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public IActionResult Login(
        [Description("Provider key returned by GET /api/auth/providers.")] string provider,
        [FromQuery][Description("Local frontend path beginning with a single slash, up to 2,048 characters. Invalid values fall back to /account.")] string? returnUrl)
    {
        if (!providerCatalog.TryGet(provider, out var selected))
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Unknown authentication provider",
                detail: "The requested authentication provider is not available.");

        return Challenge(
            new AuthenticationProperties { RedirectUri = ArchiveAuthenticationOptions.LocalReturnUrl(returnUrl) },
            selected.Scheme);
    }

    [HttpPost("logout")]
    [EndpointName("Logout")]
    [EndpointSummary("Ends the current session.")]
    [EndpointDescription("Removes the local session cookie and redirects the browser to the frontend homepage. The external provider session remains active.")]
    [ProducesResponseType(typeof(void), StatusCodes.Status302Found)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(ArchiveAuthenticationExtension.SessionScheme);
        return Redirect(options.FrontendUrl("/"));
    }
}
