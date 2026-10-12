using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Authentication;

namespace Archive.Core.Repositories.AuthenticationRepository;

public interface IAuthenticationRepository
{
    Task<ArchiveUser> GetOrCreateUserAsync(ExternalIdentityDescriptor descriptor, CancellationToken cancellationToken = default);
}
