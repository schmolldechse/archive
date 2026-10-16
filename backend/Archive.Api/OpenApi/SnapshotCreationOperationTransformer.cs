using System.Globalization;
using System.Text.Json.Nodes;
using Archive.Api.Contracts.Snapshots.Creation;
using Archive.Api.Controllers;
using Archive.Api.Options;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

/// <summary>
/// Describes the JSON and multipart actions as one OpenAPI operation.
/// MVC selects the action through Consumes; OpenAPI has one operation per path and HTTP method.
/// </summary>
public sealed class SnapshotCreationOperationTransformer(SnapshotUploadLimits limits) : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() != typeof(SnapshotsController) ||
            !action.MethodInfo.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(CreateSnapshotUrlRequest) ||
                parameter.ParameterType == typeof(CreateSnapshotFileRequest)))
            return;

        var urlSchema = await context.GetOrCreateSchemaAsync(typeof(CreateSnapshotUrlRequest), null, cancellationToken);
        var fileSchema = await context.GetOrCreateSchemaAsync(typeof(CreateSnapshotFileRequest), null, cancellationToken);

        var file = (OpenApiSchema)fileSchema.Properties!["file"];
        var maxUploadBytes = limits.MaxFileBytes.ToString("N0", CultureInfo.InvariantCulture);
        var multipartOverheadBytes = SnapshotUploadLimits.MultipartOverheadBytes.ToString("N0", CultureInfo.InvariantCulture);
        file.Description = $"Non-empty .html, .mhtml or .webarchive file. Maximum file size: {maxUploadBytes} bytes. " +
            "The extension and file contents must match; the supplied MIME type does not select the import format.";
        file.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        file.Extensions["x-max-upload-bytes"] = new JsonNodeExtension(JsonValue.Create(limits.MaxFileBytes));

        operation.OperationId = "CreateSnapshot";
        operation.Summary = "Queues a snapshot from a URL or an uploaded document.";
        operation.Description = $"Send application/json for a public HTTP(S) URL, or multipart/form-data for an HTML, MHTML or Webarchive file. " +
            $"The file limit is {maxUploadBytes} bytes; the request permits an additional {multipartOverheadBytes} bytes for multipart overhead. " +
            "A parsed file above the file limit returns 413; multipart parsing failures return 400. " +
            "Repeat the tags form field for multiple tags. HTTP 202 returns the snapshot ID, statusUrl and snapshotUrl. " +
            "Poll GET /api/snapshots/{snapshotId}/status or stream /api/snapshots/{snapshotId}/events to follow progress. " +
            "Processing ends with DISCARDED if a capture limit is reached.";
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
