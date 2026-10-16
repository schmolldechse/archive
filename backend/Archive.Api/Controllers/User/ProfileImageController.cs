using Archive.Api.Authentication;
using Archive.Api.Exceptions;
using Archive.Core.Repositories.ProfileImageRepository;
using Archive.Core.Models.ProfileImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel;
using System.Security.Cryptography;

namespace Archive.Api.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("User Profile Images")]
[Description("Stored user profile images for account and public archive attribution.")]
[Authorize]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
public sealed class ProfileImageController(IProfileImageRepository profileImageRepository) : ControllerBase
{
    [HttpGet("profile-image")]
    [EndpointName("GetCurrentUserProfileImage")]
    [EndpointSummary("Returns the current user profile image.")]
    [EndpointDescription("Requires a session cookie, including during registration. Returns the stored WebP image with an ETag. If-None-Match returns HTTP 304 when unchanged. A missing image returns HTTP 404.")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/webp")]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(void), StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await profileImageRepository.GetByUserIdAsync(User.ReadUserId(), cancellationToken);
        return result switch
        {
            ProfileImageResult.Found found => ServeImage(found.Content, "private, no-cache"),
            ProfileImageResult.UserNotFound => throw new InvalidSessionException(),
            ProfileImageResult.Unavailable => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile image unavailable",
                detail: "No profile image is stored for the current user."),
            _ => throw new InvalidOperationException("Unknown profile image result.")
        };
    }

    [AllowAnonymous]
    [EnableCors("public")]
    [HttpGet("{handle}/profile-image")]
    [EndpointName("GetUserProfileImage")]
    [EndpointSummary("Returns a registered user's public profile image.")]
    [EndpointDescription("Returns the stored WebP image for a registered user's current handle. No session is required. Unknown or incomplete users and missing images return HTTP 404. If-None-Match returns HTTP 304 when unchanged.")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/webp")]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified)]
    [ProducesResponseType(typeof(void), StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetUserProfileImage(
        [Description("Current unique lowercase handle of a user with a completed registration.")] string handle,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await profileImageRepository.GetByHandleAsync(handle, cancellationToken);
        return result is ProfileImageResult.Found found
            ? ServeImage(found.Content, "public, no-cache")
            : Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile image unavailable",
                detail: "No public profile image is available for this handle.");
    }

    private FileContentResult ServeImage(byte[] content, string cacheControl)
    {
        Response.Headers.CacheControl = cacheControl;
        Response.Headers.XContentTypeOptions = "nosniff";
        var hash = Convert.ToHexStringLower(SHA256.HashData(content));
        return File(content, "image/webp", lastModified: null, entityTag: new EntityTagHeaderValue($"\"{hash}\""));
    }
}
