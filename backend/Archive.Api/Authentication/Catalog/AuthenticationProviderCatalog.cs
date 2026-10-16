using Archive.Api.Authentication.Provider;

namespace Archive.Api.Authentication.Catalog;

public sealed class AuthenticationProviderCatalog : IAuthenticationProviderCatalog
{
    private readonly Dictionary<string, AuthenticationProviderDescriptor> _providers;

    public AuthenticationProviderCatalog(IEnumerable<IAuthenticationProvider> providers)
    {
        var descriptors = providers
            .Select(provider => new AuthenticationProviderDescriptor(provider.Key, provider.DisplayName, provider.Scheme))
            .ToArray();
        _providers = descriptors.ToDictionary(provider => provider.Key, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string providerName, out AuthenticationProviderDescriptor descriptor)
    {
        descriptor = null!;
        return !string.IsNullOrWhiteSpace(providerName) && _providers.TryGetValue(providerName, out descriptor!);
    }

    public IReadOnlyCollection<AuthenticationProviderDescriptor> GetAll() => _providers.Values;
}
