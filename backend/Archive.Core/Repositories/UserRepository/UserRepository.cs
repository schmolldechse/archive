using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Models.Users;
using Archive.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Repositories.UserRepository;

internal sealed class UserRepository(DataContext dataContext) : IUserRepository
{
    public async Task<ArchiveUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => await dataContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<ArchiveUser?> GetByHandleAsync(string handle, CancellationToken cancellationToken = default) => await dataContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Handle == handle, cancellationToken);

    public async Task<bool> HandleExistsAsync(string handle, CancellationToken cancellationToken = default) => await dataContext.Users
        .AnyAsync(user => user.Handle == handle, cancellationToken);

    public async Task<UserProfileUpdateResult> UpdateProfileAsync(Guid userId, UserProfileUpdate profile, CancellationToken cancellationToken = default)
    {
        var validationErrors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(profile, new ValidationContext(profile), validationErrors, validateAllProperties: true))
        {
            var errors = validationErrors
                .SelectMany(error => error.MemberNames.Select(field => new FieldValidationError(field, error.ErrorMessage!)))
                .ToList();

            return new UserProfileUpdateResult.Invalid(errors);
        }

        var user = await dataContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user is null)
            return new UserProfileUpdateResult.NotFound();

        var handleExists = await dataContext.Users.AnyAsync(
            other => other.Handle == profile.Handle && other.Id != userId, cancellationToken);
        if (handleExists)
            return new UserProfileUpdateResult.HandleUnavailable();

        user.DisplayName = profile.DisplayName;
        user.Handle = profile.Handle;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.RegistrationCompletedAt ??= user.UpdatedAt;

        await dataContext.SaveChangesAsync(cancellationToken);

        return new UserProfileUpdateResult.Updated(user);
    }
}
