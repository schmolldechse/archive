using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;
using Archive.Core.Models.Users;
using Archive.Core.Models;
using Npgsql;

namespace Archive.Core.Repositories.UserRepository;

internal sealed partial class UserRepository(DataContext dataContext) : IUserRepository
{
    public async Task<ArchiveUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => await dataContext.Users
        .AsNoTracking()
        .Include(user => user.Identities)
        .Include(user => user.PermissionOverrides)
        .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public async Task<ArchiveUser?> GetByHandleAsync(string handle, CancellationToken cancellationToken = default) => await dataContext.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(user => user.Handle == handle, cancellationToken);

    public async Task<bool> HandleExistsAsync(string handle, CancellationToken cancellationToken = default) => await dataContext.Users
        .AnyAsync(user => user.Handle == handle, cancellationToken);

    public async Task<UserProfileUpdateResult> UpdateProfileAsync(Guid userId, UserProfileUpdate profile, CancellationToken cancellationToken = default)
    {
        var errors = ModelValidation.GetErrors(profile);
        if (errors.Count > 0)
            return new UserProfileUpdateResult.Invalid(errors);

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

        try
        {
            await dataContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsHandleConflict(exception))
        {
            dataContext.Entry(user).State = EntityState.Detached;
            return new UserProfileUpdateResult.HandleUnavailable();
        }

        return new UserProfileUpdateResult.Updated(user);
    }

    private bool IsHandleConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } conflict)
            return false;
        var entity = dataContext.Model.FindEntityType(typeof(ArchiveUser))!;
        var index = entity.GetIndexes().Single(index =>
            index.Properties.Count == 1 && index.Properties[0].Name == nameof(ArchiveUser.Handle));
        return conflict.SchemaName == entity.GetSchema() && conflict.TableName == entity.GetTableName() &&
            conflict.ConstraintName == index.GetDatabaseName();
    }
}
