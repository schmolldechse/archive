using Archive.Api.Authentication;
using Archive.Api.Authentication.Catalog;
using Archive.Api.Contracts;
using Archive.Api.Mapping;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

[ApiController, Route("api/auth"), Tags("Authentication"), AllowAnonymous, EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
public sealed class AuthenticationController(IAuthenticationProviderCatalog providers,
    ArchiveAuthenticationOptions options, AuthenticationMapper mapper) : ControllerBase
{
    [HttpGet("providers"), EndpointName("ListAuthenticationProviders"), EndpointSummary("Lists available external sign-in providers.")]
    [EndpointDescription("Lists providers available for voluntary sign-in. Anonymous archive use remains available.")]
    [ProducesResponseType<AuthenticationProviderResponse[]>(200)]
    public ActionResult<AuthenticationProviderResponse[]> GetProvider() => providers.GetAll().Select(provider =>
        mapper.ToResponse(provider, $"{options.PublicApiOrigin.TrimEnd('/')}/api/auth/login/{provider.Key}")).ToArray();

    [HttpGet("session"), EndpointName("GetAuthenticationSession"), EndpointSummary("Reads the current optional session.")]
    [EndpointDescription("Returns HTTP 200 for both authenticated and anonymous visitors. Never redirects to Google.")]
    [ProducesResponseType<AuthenticationSessionResponse>(200)]
    [ProducesResponseType<AuthenticationProblemResponse>(503)]
    public ActionResult<AuthenticationSessionResponse> Session()
    {
        var account = ArchiveAuthenticationExtension.CurrentUser(HttpContext);
        return new AuthenticationSessionResponse(account is not null, account is null ? null :
            mapper.ToResponse(account, ArchiveAuthenticationExtension.ImageUrl(options, account)));
    }

    [HttpPost("login/{provider}"), Consumes("application/x-www-form-urlencoded")]
    [EndpointName("BeginExternalLogin")]
    [EndpointSummary("Starts external sign-in with a normal browser form.")]
    [EndpointDescription("Redirects the browser to the configured provider. returnUrl is a local frontend path, never an external URL.")]
    [ProducesResponseType(302), ProducesResponseType<AuthenticationProblemResponse>(400), ProducesResponseType<AuthenticationProblemResponse>(404)]
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

    [HttpPost("logout"), EndpointName("Logout"), EndpointSummary("Ends the current local session.")]
    [EndpointDescription("Deletes the API session cookie and redirects to the frontend homepage. Does not log out of the external provider.")]
    [ProducesResponseType(302)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(ArchiveAuthenticationExtension.SessionScheme);
        return Redirect(options.FrontendUrl("/"));
    }

    private ObjectResult Error(int status, string code, string detail) => Problem(detail, statusCode: status,
        extensions: new Dictionary<string, object?> { ["code"] = code, ["traceId"] = HttpContext.TraceIdentifier });
}
