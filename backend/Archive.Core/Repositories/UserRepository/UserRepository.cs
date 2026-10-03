using Archive.Core.Entities.Authentication;
using Microsoft.EntityFrameworkCore;

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

    public async Task<ArchiveUser> UpdateUserAsync(Guid userId, ArchiveUser updatedUser, CancellationToken cancellationToken = default)
    {
        var user = await dataContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
        if (user == null) throw new InvalidOperationException("This user does not exist.");

        user.Handle = updatedUser.Handle;
        user.DisplayName = updatedUser.DisplayName;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        user.ProfileImage = updatedUser.ProfileImage;

        await dataContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}
