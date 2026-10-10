using Archive.Api.Authentication;
using Archive.Api.Contracts.Authentication;
using Archive.Api.Exceptions;
using Archive.Api.Mapping;
using Archive.Core.Repositories.UserRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel;
using System.Text;

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

        try
        {
            request.DisplayName = request.DisplayName.Trim().Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError("displayName", "Enter a valid display name.");
            return ValidationProblem(ModelState);
        }

        request.Handle = request.Handle.Trim().ToLowerInvariant();
        ModelState.ClearValidationState(string.Empty);
        if (!TryValidateModel(request))
            return ValidationProblem(ModelState);

        var current = await userRepository.GetByIdAsync(User.ReadUserId(), cancellationToken)
            ?? throw new InvalidSessionException();
        var owner = await userRepository.GetByHandleAsync(request.Handle, cancellationToken);
        if (owner is not null && owner.Id != current.Id)
            return HandleUnavailable();

        var updated = mapper.ToModel(request);
        updated.ProfileImage = current.ProfileImage;

        try
        {
            var account = await userRepository.UpdateUserAsync(current.Id, updated, cancellationToken);
            return Ok(mapper.ToResponse(account, account.GetProfileImageUrl(options.PublicApiOrigin)));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_users_handle" })
        {
            return HandleUnavailable();
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
