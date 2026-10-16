using Archive.Api.Authentication;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Exceptions;
using Archive.Api.Mapping;
using Archive.Api.Validation;
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
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<UserProfileResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var account = await userRepository.GetByIdAsync(User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();

        return Ok(mapper.ToResponse(account, account.GetProfileImageUrl(options.PublicApiOrigin)));
    }

    [HttpPut("profile")]
    [Consumes("application/json")]
    [EndpointName("UpdateCurrentUser")]
    [EndpointSummary("Updates the current user profile.")]
    [EndpointDescription("Requires a session cookie. Saves the normalized display name and handle and completes registration. An unavailable handle returns HTTP 409 with a handle field error.")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<UserProfileResponse>> UpdateCurrentUser(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
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
                ModelState.AddErrors(invalid.Errors);
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
