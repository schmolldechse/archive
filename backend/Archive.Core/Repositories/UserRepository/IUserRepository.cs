using Archive.Core.Entities.Authentication;

namespace Archive.Core.Repositories.UserRepository;

public interface IUserRepository
{
    Task<ArchiveUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ArchiveUser?> GetByHandleAsync(string handle, CancellationToken cancellationToken = default);
    Task<bool> HandleExistsAsync(string handle, CancellationToken cancellationToken = default);
    Task<ArchiveUser> UpdateUserAsync(Guid userId, ArchiveUser updatedUser, CancellationToken cancellationToken = default);
}
