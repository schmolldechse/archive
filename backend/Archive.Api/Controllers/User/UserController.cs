using Archive.Api.Authentication;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Exceptions;
using Archive.Api.Mapping;
using Archive.Core.Repositories.UserRepository;
using Archive.Core.Models.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Archive.Api.Controllers.User;

[ApiController]
[Route("api/users")]
[Tags("Users")]
[Description("Profile information for the current user.")]
[Authorize]
[EnableCors(ArchiveAuthenticationExtension.CorsPolicy)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
public sealed class UserController(
    IUserRepository userRepository,
    ArchiveAuthenticationOptions options,
    AuthenticationMapper mapper) : ControllerBase
{
    [HttpGet("profile")]
    [EndpointName("GetCurrentUser")]
    [EndpointSummary("Returns the current user profile.")]
    [EndpointDescription("Requires a browser session cookie. Returns the authenticated user's display name, handle and profile image URL.")]
    [ProducesResponseType<AuthenticatedArchiveUser>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<AuthenticatedArchiveUser>> GetUser(CancellationToken cancellationToken)
    {
        var account = await userRepository.GetByIdAsync(User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();

        return Ok(mapper.ToResponse(account, account.GetProfileImageUrl(options.PublicApiOrigin)));
    }

    [HttpPut("profile")]
    [Consumes("application/json")]
    [EndpointName("UpdateCurrentUser")]
    [EndpointSummary("Updates the current user profile.")]
    [EndpointDescription("Requires a browser session cookie. Updates the display name and handle. Whitespace is trimmed, the display name is normalized to Unicode NFC, and the handle is stored in lowercase. Handles are unique; an unavailable handle returns HTTP 409 with field validation errors.")]
    [ProducesResponseType<AuthenticatedArchiveUser>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<AuthenticatedArchiveUser>> UpdateUser(
        [FromBody] UpdateUserProfile request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await userRepository.UpdateProfileAsync(User.ReadUserId(),
            new(request.DisplayName, request.Handle), cancellationToken);
        switch (result)
        {
            case UserProfileUpdateResult.Updated updated:
                return Ok(mapper.ToResponse(updated.User, updated.User.GetProfileImageUrl(options.PublicApiOrigin)));
            case UserProfileUpdateResult.NotFound:
                throw new InvalidSessionException();
            case UserProfileUpdateResult.HandleUnavailable:
                return HandleUnavailable();
            case UserProfileUpdateResult.Invalid invalid:
                foreach (var error in invalid.Errors)
                    ModelState.AddModelError(error.Field, error.Message);
                return ValidationProblem(ModelState);
            default:
                throw new InvalidOperationException("Unknown profile update result.");
        }
    }

    private ActionResult HandleUnavailable()
    {
        ModelState.AddModelError("handle", "This handle is already in use.");
        return ValidationProblem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Handle unavailable",
            modelStateDictionary: ModelState);
    }
}
