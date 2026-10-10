using Archive.Api.Authentication;
using Archive.Api.Exceptions;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SkiaSharp;
using System.ComponentModel;
using System.Security.Cryptography;

namespace Archive.Api.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("User Profile Images")]
[Description("Provider profile images for the current user.")]
[Authorize]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class ProfileImageController(IUserRepository userRepository) : ControllerBase
{
    [HttpGet("profile-image")]
    [EndpointName("GetCurrentUserProfileImage")]
    [EndpointSummary("Returns the current user profile image.")]
    [EndpointDescription("Returns the stored provider profile image encoded as WebP. Requests with a matching If-None-Match header return HTTP 304. A missing or unreadable image returns HTTP 404.")]
    [Produces("image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetProfileImage(CancellationToken cancellationToken)
    {
        var account = await userRepository.GetByIdAsync(User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();

        using var source = new MemoryStream(account.ProfileImage ?? [], writable: false);
        using var bitmap = SKBitmap.Decode(source);
        if (bitmap is null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Profile image unavailable",
                detail: "No readable provider profile image is stored for the current user.");

        using var pixels = bitmap.PeekPixels();
        using var encoded = pixels.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100))
            ?? throw new InvalidOperationException("The profile image could not be encoded as WebP.");
        var bytes = encoded.ToArray();

        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.XContentTypeOptions = "nosniff";
        var entityTag = new EntityTagHeaderValue($"\"{Convert.ToHexStringLower(SHA256.HashData(bytes))}\"");
        return File(bytes, "image/webp", lastModified: null, entityTag: entityTag);
    }
}
