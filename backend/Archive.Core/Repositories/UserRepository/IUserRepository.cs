using Archive.Core.Entities.Authentication;

using Archive.Core.Models.Users;

namespace Archive.Core.Repositories.UserRepository;

public interface IUserRepository
{
    /// <summary>Searches registered accounts by handle, display name, or role.</summary>
    Task<UserListPage> SearchAsync(string? query, Entities.Authentication.Permissible.ArchiveRole? role, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Loads current access settings for a registered account.</summary>
    Task<ArchiveUser?> GetAccessAsync(string handle, CancellationToken cancellationToken = default);

    /// <summary>Rechecks actor access and applies all changes under one transaction.</summary>
    Task<UserAccessUpdateResult> UpdateAccessAsync(Guid actorId, string handle, string expectedTag, UserAccessUpdate update, CancellationToken cancellationToken = default);

    Task<ArchiveUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ArchiveUser?> GetByHandleAsync(string handle, CancellationToken cancellationToken = default);
    Task<bool> HandleExistsAsync(string handle, CancellationToken cancellationToken = default);
    Task<UserProfileUpdateResult> UpdateProfileAsync(Guid userId, UserProfileUpdate profile, CancellationToken cancellationToken = default);
}
