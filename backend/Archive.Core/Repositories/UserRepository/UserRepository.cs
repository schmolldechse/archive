using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;

using Archive.Core.Models.Users;
using Archive.Core.Models;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Text;

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
        string displayName;
        try
        {
            displayName = (profile.DisplayName ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return new UserProfileUpdateResult.Invalid([new("displayName", "Enter a valid display name.")]);
        }

        var normalized = new UserProfileUpdate(displayName, (profile.Handle ?? string.Empty).Trim().ToLowerInvariant());
        var validation = new List<ValidationResult>();
        if (!Validator.TryValidateObject(normalized, new ValidationContext(normalized), validation, validateAllProperties: true))
            return new UserProfileUpdateResult.Invalid(validation.SelectMany(error => error.MemberNames
                .Select(field => new FieldValidationError(field, error.ErrorMessage!))).ToArray());

        var user = await dataContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null) return new UserProfileUpdateResult.NotFound();
        if (await dataContext.Users.AnyAsync(item => item.Handle == normalized.Handle && item.Id != userId, cancellationToken))
            return new UserProfileUpdateResult.HandleUnavailable();

        user.DisplayName = normalized.DisplayName;
        user.Handle = normalized.Handle;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dataContext.SaveChangesAsync(cancellationToken);
        return new UserProfileUpdateResult.Updated(user);
    }
}
