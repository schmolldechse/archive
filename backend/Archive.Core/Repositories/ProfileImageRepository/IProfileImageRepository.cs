using Archive.Core.Models.ProfileImages;

namespace Archive.Core.Repositories.ProfileImageRepository;

public interface IProfileImageRepository
{
    Task<ProfileImageResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
