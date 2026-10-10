using Archive.Api.Authentication;
using Archive.Api.Exceptions;
using Archive.Core.Repositories.ProfileImageRepository;
using Archive.Core.Models.ProfileImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel;

namespace Archive.Api.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("User Profile Images")]
[Description("Profile images for the authenticated user.")]
[Authorize]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class ProfileImageController(IProfileImageRepository profileImageRepository) : ControllerBase
{
    [HttpGet("profile-image")]
    [EndpointName("GetCurrentUserProfileImage")]
    [EndpointSummary("Returns the current user profile image.")]
    [EndpointDescription("Requires a browser session cookie. Returns a WebP profile image and an ETag. Send the ETag in If-None-Match to receive HTTP 304 without a body when unchanged. A missing or unreadable image returns HTTP 404.")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "image/webp")]
    [ProducesResponseType(typeof(void), StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var result = await profileImageRepository.GetByUserIdAsync(User.ReadUserId(), cancellationToken);
        if (result is ProfileImageResult.UserNotFound)
            throw new InvalidSessionException();
        if (result is ProfileImageResult.Unavailable)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile image unavailable",
                detail: "No readable provider profile image is stored for the current user.");
        var image = ((ProfileImageResult.Found)result).Image;

        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.XContentTypeOptions = "nosniff";
        var entityTag = new EntityTagHeaderValue($"\"{image.ContentHash}\"");
        return File(image.Content, "image/webp", lastModified: null, entityTag: entityTag);
    }
}
