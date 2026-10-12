using System.Globalization;
using System.Text.Json.Nodes;
using Archive.Api.Contracts.Archive.Creation;
using Archive.Api.Controllers;
using Archive.Core.Options;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

/// <summary>
/// Describes the JSON and multipart actions as one OpenAPI operation.
/// MVC selects the action through Consumes; OpenAPI has one operation per path and HTTP method.
/// </summary>
public sealed class ArchiveCreationOperationTransformer(ArchiveOptions options) : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() != typeof(ArchiveController) ||
            !action.MethodInfo.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(CreateArchiveUrlRequest) ||
                parameter.ParameterType == typeof(CreateArchiveFileRequest)))
            return;

        // Use the framework's schema service so validation, property names and references stay intact.
        var urlSchema = await context.GetOrCreateSchemaAsync(typeof(CreateArchiveUrlRequest), null, cancellationToken);
        var fileSchema = await context.GetOrCreateSchemaAsync(typeof(CreateArchiveFileRequest), null, cancellationToken);

        var file = (OpenApiSchema)fileSchema.Properties!["file"];
        var maxUploadBytes = options.MaxUploadBytes.ToString("N0", CultureInfo.InvariantCulture);
        file.Description = $"Non-empty .html, .mhtml or .webarchive file. Maximum file size: {maxUploadBytes} bytes. " +
            "The extension and file contents must match; the supplied MIME type does not select the import format.";
        file.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        file.Extensions["x-max-upload-bytes"] = new JsonNodeExtension(JsonValue.Create(options.MaxUploadBytes));

        operation.OperationId = "CreateArchive";
        operation.Summary = "Queues an archive from a URL or an uploaded document.";
        operation.Description = $"Send application/json for a public HTTP(S) URL, or multipart/form-data for an HTML, MHTML or Webarchive file. " +
            $"The file limit is {maxUploadBytes} bytes; the request permits an additional 1,000,000 bytes for multipart overhead. " +
            "A parsed file above the file limit returns 413; multipart parsing failures return 400. " +
            "Repeat the tags form field for multiple tags. The response describes the queued job, not a completed snapshot. " +
            "Poll GET /api/archive/{archiveId} or use its /events stream to follow progress. " +
            "Capture duration, snapshot storage and resource limits are configured separately on the worker. " +
            "A job may be discarded when a worker limit is reached.";
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = urlSchema,
                    Examples = new Dictionary<string, IOpenApiExample>
                    {
                        ["publicPage"] = new OpenApiExample
                        {
                            Summary = "Archive a public page with metadata",
                            Value = JsonNode.Parse("""
                                {"sourceUrl":"https://example.org/","title":"Example page","description":"A public page captured for reference.","tags":["reference","web"]}
                                """)
                        }
                    }
                },
                ["multipart/form-data"] = new()
                {
                    Schema = fileSchema,
                    Encoding = new Dictionary<string, OpenApiEncoding>
                    {
                        ["tags"] = new() { Style = ParameterStyle.Form, Explode = true }
                    }
                }
            }
        };
    }
}
