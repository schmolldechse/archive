using Archive.Core.Entities.Authentication;
using Archive.Core.Models.Authentication;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Archive.Core.Repositories.AuthenticationRepository;

public sealed class AuthenticationRepository(DataContext dataContext) : IAuthenticationRepository
{
    public async Task<ArchiveUser> GetOrCreateUserAsync(ExternalIdentityDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var authIdentity = await dataContext.UserAuthIdentities
            .Include(identity => identity.User)
            .Include(identity => identity.AuthProvider)
            .SingleOrDefaultAsync(identity => identity.AuthProvider.ProviderKey == descriptor.ProviderKey && identity.ExternalSubject == descriptor.ExternalSubject, cancellationToken);
        if (authIdentity is not null)
        {
            authIdentity.LastLoginAt = timestamp;
            authIdentity.User.UpdatedAt = timestamp;

            await dataContext.SaveChangesAsync(cancellationToken);
            return authIdentity.User;
        }

        var authProvider = await dataContext.AuthProviders.SingleOrDefaultAsync(provider => provider.ProviderKey == descriptor.ProviderKey, cancellationToken);
        if (authProvider is null)
        {
            authProvider = new AuthProvider
            {
                ProviderKey = descriptor.ProviderKey,
                DisplayName = descriptor.ProviderDisplayName,
            };
            dataContext.AuthProviders.Add(authProvider);
        }

        var displayName = TrimToMax(
            string.IsNullOrWhiteSpace(descriptor.DisplayName) ? "User" : descriptor.DisplayName.Trim(),
            64);

        var user = new ArchiveUser
        {
            DisplayName = displayName,
            Handle = "",
            ProfileImage = descriptor.ProfilePicture is { Length: > 0 } ? descriptor.ProfilePicture : null
        };
        authIdentity = new UserAuthIdentity
        {
            User = user,
            AuthProvider = authProvider,
            ExternalSubject = descriptor.ExternalSubject,
        };

        dataContext.UserAuthIdentities.Add(authIdentity);
        await dataContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string TrimToMax(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
