using Archive.Api.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class AuthenticationDocumentTransformer(ArchiveAuthenticationOptions options, IWebHostEnvironment environment)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Servers = [new OpenApiServer { Url = options.PublicApiOrigin.TrimEnd('/') }];
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["sessionCookie"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Cookie,
            Name = environment.IsDevelopment() ? "archive.session" : "__Host-archive.session",
            Description = "Optional local cookie session. Required only for the current user's profile endpoints."
        };
        foreach (var (path, item) in document.Paths)
        {
            if (!path.StartsWith("/api/users/me", StringComparison.Ordinal) || item.Operations is null) continue;
            foreach (var operation in item.Operations.Values)
            {
                operation.Security = [new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("sessionCookie", document)] = []
                }];
            }
        }
        return Task.CompletedTask;
    }
}
