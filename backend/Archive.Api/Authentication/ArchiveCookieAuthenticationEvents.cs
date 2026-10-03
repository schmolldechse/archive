using System.Security.Claims;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Archive.Api.Authentication;

public sealed class ArchiveCookieAuthenticationEvents(IUserRepository users) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        if (context.Request.Path != "/api/auth/session" && !context.Request.Path.StartsWithSegments("/api/users/me"))
            return;

        var user = Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await users.GetByIdAsync(id, context.HttpContext.RequestAborted)
            : null;
        if (user is null)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(ArchiveAuthenticationExtension.SessionScheme);
            return;
        }

        context.HttpContext.Items[ArchiveAuthenticationExtension.CurrentUserKey] = user;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context) =>
        Problem(context.HttpContext, 401, "authentication_required", "Sign in to access your profile.");

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        Problem(context.HttpContext, 403, "access_denied", "You cannot access this resource.");

    private static Task Problem(HttpContext context, int status, string code, string detail) =>
        Results.Problem(statusCode: status, detail: detail, extensions: new Dictionary<string, object?>
        {
            ["code"] = code,
            ["traceId"] = context.TraceIdentifier
        }).ExecuteAsync(context);
}
