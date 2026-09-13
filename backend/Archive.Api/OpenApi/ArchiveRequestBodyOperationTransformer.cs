using Archive.Api.Contracts;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class ArchiveRequestBodyOperationTransformer : IOpenApiOperationTransformer
{
    private const string UrlRequestSchemaName = nameof(CreateArchiveUrlRequest);
    private const string FileRequestSchemaName = nameof(CreateArchiveFileRequest);

    public async Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(context.Description.HttpMethod, HttpMethods.Post, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(context.Description.RelativePath, "api/archive", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var urlRequestSchema = await context.GetOrCreateSchemaAsync(
            typeof(CreateArchiveUrlRequest),
            parameterDescription: null,
            cancellationToken);
        var fileRequestSchema = await context.GetOrCreateSchemaAsync(
            typeof(CreateArchiveFileRequest),
            parameterDescription: null,
            cancellationToken);

        var document = context.Document ?? throw new InvalidOperationException("The OpenAPI document is unavailable.");
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
        document.Components.Schemas[UrlRequestSchemaName] = urlRequestSchema;
        document.Components.Schemas[FileRequestSchemaName] = fileRequestSchema;

        operation.OperationId = "CreateArchive";
        operation.Summary = "Creates an archive from a URL or an HTML file.";
        operation.Description = "Accepts either a JSON URL request or an HTML file with metadata as multipart form data and queues a new archive.";
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = new OpenApiSchemaReference(UrlRequestSchemaName, document)
                },
                ["multipart/form-data"] = new()
                {
                    Schema = new OpenApiSchemaReference(FileRequestSchemaName, document)
                }
            }
        };
    }
}
