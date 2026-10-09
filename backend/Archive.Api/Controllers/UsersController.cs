using Archive.Api.Authentication;
using Archive.Api.Authentication.Provider;
using Archive.Api.Contracts;
using Archive.Api.Mapping;
using Archive.Core.Entities.Authentication;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Archive.Api.Controllers;

[ApiController, Route("api/users/me"), Tags("Users"), Authorize, EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(401, "application/problem+json"), ProducesResponseType<ProblemDetails>(503, "application/problem+json")]
public sealed class UsersController(IUserRepository users,
    ArchiveAuthenticationOptions options, AuthenticationMapper mapper) : ControllerBase
{
    [HttpGet, EndpointName("GetCurrentUser"), EndpointSummary("Reads the authenticated user's profile."), ProducesResponseType<UserResponse>(200)]
    [EndpointDescription("The user ID comes from the local session. The response contains no external identities or image bytes.")]
    public ActionResult<UserResponse> Get() => ResponseFor(ArchiveAuthenticationExtension.CurrentUser(HttpContext)!);

    [HttpPut, Consumes("application/json"), EndpointSummary("Updates the authenticated user's display name and handle.")]
    [EndpointName("UpdateCurrentUser")]
    [EndpointDescription("Normalizes the name to NFC and the handle to lowercase. A database constraint enforces handle uniqueness.")]
    [ProducesResponseType<UserResponse>(200), ProducesResponseType<ProblemDetails>(400, "application/problem+json"), ProducesResponseType<ProblemDetails>(409, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(415, "application/problem+json")]
    public async Task<ActionResult<UserResponse>> Update([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request is null)
            return Error(400, "invalid_profile", "Enter a valid display name and handle.");
        var current = ArchiveAuthenticationExtension.CurrentUser(HttpContext)!;
        var updated = mapper.ToModel(request);
        updated.ProfileImage = current.ProfileImage;
        try
        {
            updated.DisplayName = updated.DisplayName.Trim().Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return Error(400, "invalid_profile", "Enter a valid display name.");
        }
        updated.Handle = updated.Handle.Trim().ToLowerInvariant();
        if (updated.DisplayName.Length is < 1 or > 64 || updated.DisplayName.Any(char.IsControl))
            return Error(400, "invalid_profile", "Enter a display name of 1–64 characters without controls.",
                new() { ["displayName"] = ["Enter a display name of 1–64 characters without controls."] });
        if (!Regex.IsMatch(updated.Handle, "^[a-z0-9][a-z0-9_-]{1,30}[a-z0-9]$", RegexOptions.CultureInvariant))
            return Error(400, "invalid_profile", "Enter a valid handle of 3–32 characters.",
                new() { ["handle"] = ["Use letters or digits, with hyphens or underscores between them."] });
        if (await users.GetByHandleAsync(updated.Handle, cancellationToken) is { } owner && owner.Id != current.Id)
            return HandleUnavailable();
        try
        {
            var account = await users.UpdateUserAsync(current.Id, updated, cancellationToken);
            return ResponseFor(account);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_users_handle" })
        {
            return HandleUnavailable();
        }
    }

    [HttpGet("profile-image"), EndpointSummary("Reads the current user's locally stored provider image.")]
    [EndpointName("GetCurrentUserProfileImage")]
    [EndpointDescription("Returns the provider image imported at first sign-in as JPEG, PNG or WebP bytes. Private caching uses a SHA-256 ETag; a matching If-None-Match returns 304.")]
    [Produces("image/jpeg", "image/png", "image/webp"), ProducesResponseType(200), ProducesResponseType(304)]
    [ProducesResponseType<ProblemDetails>(404, "application/problem+json")]
    public IActionResult GetImage()
    {
        var image = ArchiveAuthenticationExtension.CurrentUser(HttpContext)!.ProfileImage;
        if (image is not { Length: > 0 } || ProfileImageContent.ContentType(image) is not { } contentType)
            return Error(404, "profile_image_not_found", "No profile image is stored.");
        var etag = $"\"{Convert.ToHexStringLower(SHA256.HashData(image))}\"";
        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.ETag = etag;
        Response.Headers.XContentTypeOptions = "nosniff";
        if (Request.Headers.IfNoneMatch.ToString().Split(',').Any(value =>
            value.Trim() is "*" || value.Trim().Replace("W/", "", StringComparison.Ordinal) == etag))
            return StatusCode(304);
        return File(image, contentType);
    }

    private UserResponse ResponseFor(ArchiveUser account) =>
        mapper.ToResponse(account, ArchiveAuthenticationExtension.ImageUrl(options, account));

    private ObjectResult HandleUnavailable() => Error(409, "handle_unavailable", "This handle is already in use.",
        new() { ["handle"] = ["Choose a different handle."] });

    private ObjectResult Error(int status, string code, string detail, Dictionary<string, string[]>? errors = null) =>
        Problem(detail, statusCode: status, extensions: new Dictionary<string, object?>
        {
            ["code"] = code,
            ["traceId"] = HttpContext.TraceIdentifier,
            ["errors"] = errors
        });
}
