namespace Archive.Api.Authentication.Catalog;

public interface IAuthenticationProviderCatalog
{
    bool TryGet(string providerName, out AuthenticationProviderDescriptor descriptor);
    IReadOnlyCollection<AuthenticationProviderDescriptor> GetAll();
}
