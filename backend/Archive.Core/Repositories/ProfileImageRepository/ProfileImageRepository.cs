using Archive.Core.Models.ProfileImages;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.ProfileImageRepository;

internal sealed class ProfileImageRepository(DataContext dataContext) : IProfileImageRepository
{
    public async Task<ProfileImageResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dataContext.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.ProfileImage })
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? new ProfileImageResult.UserNotFound() : ReadImage(user.ProfileImage);
    }

    public async Task<ProfileImageResult> GetByHandleAsync(string handle, CancellationToken cancellationToken = default)
    {
        var user = await dataContext.Users.AsNoTracking()
            .Where(user => user.Handle == handle && user.RegistrationCompletedAt != null)
            .Select(user => new { user.ProfileImage })
            .SingleOrDefaultAsync(cancellationToken);
        return user is null ? new ProfileImageResult.UserNotFound() : ReadImage(user.ProfileImage);
    }

    private static ProfileImageResult ReadImage(byte[]? content) => content is { Length: > 0 }
        ? new ProfileImageResult.Found(content)
        : new ProfileImageResult.Unavailable();
}
