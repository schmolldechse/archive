using Microsoft.AspNetCore.Authentication;

namespace Archive.Api.Authentication.Provider;

public interface IAuthenticationProvider
{
    string Id { get; }
    string DisplayName { get; }
    string Scheme { get; }
    void AddAuthentication(AuthenticationBuilder authentication, bool isDevelopment);
}
