namespace Archive.Api.Authentication.Catalog;

public interface IAuthenticationProviderCatalog
{
    bool TryGet(string providerName, out AuthProviderDescriptor descriptor);
    IReadOnlyCollection<AuthProviderDescriptor> GetAll();
}
