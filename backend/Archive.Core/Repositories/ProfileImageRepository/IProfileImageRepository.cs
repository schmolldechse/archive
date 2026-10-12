using Archive.Core.Models.ProfileImages;

namespace Archive.Core.Repositories.ProfileImageRepository;

public interface IProfileImageRepository
{
    Task<ProfileImageResult> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileImageResult> GetByHandleAsync(string handle, CancellationToken cancellationToken = default);
}
