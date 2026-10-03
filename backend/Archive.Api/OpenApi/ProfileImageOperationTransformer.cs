using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class ProfileImageOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.RelativePath?.StartsWith("api/auth/", StringComparison.Ordinal) == true ||
            context.Description.RelativePath?.StartsWith("api/users/me", StringComparison.Ordinal) == true)
        {
            foreach (var (status, documentedResponse) in operation.Responses ?? [])
            {
                if (int.TryParse(status, out var code) && code >= 400 && documentedResponse.Content is { Count: > 0 } content)
                {
                    var schema = content.Values.First().Schema;
                    content.Clear();
                    content.Add("application/problem+json", new OpenApiMediaType { Schema = schema });
                }
                else if (status is "302" or "304") documentedResponse.Content?.Clear();
            }
        }
        if (context.Description.RelativePath != "api/users/me/profile-image") return Task.CompletedTask;
        if (context.Description.HttpMethod == "GET" && operation.Responses?.TryGetValue("200", out var response) == true)
            foreach (var media in response.Content?.Values ?? [])
                media.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
        return Task.CompletedTask;
    }
}
