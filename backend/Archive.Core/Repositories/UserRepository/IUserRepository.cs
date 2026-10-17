using Archive.Core.Entities.Authentication;

using Archive.Core.Models.Users;

namespace Archive.Core.Repositories.UserRepository;

public interface IUserRepository
{
    Task<ArchiveUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ArchiveUser?> GetByHandleAsync(string handle, CancellationToken cancellationToken = default);
    Task<bool> HandleExistsAsync(string handle, CancellationToken cancellationToken = default);
    Task<UserProfileUpdateResult> UpdateProfileAsync(Guid userId, UserProfileUpdate profile, CancellationToken cancellationToken = default);
}
