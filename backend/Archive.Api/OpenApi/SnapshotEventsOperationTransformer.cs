using System.Text.Json.Nodes;
using Archive.Api.Contracts.Snapshots.Processing;
using Archive.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class SnapshotEventsOperationTransformer : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() != typeof(SnapshotsController) ||
            action.MethodInfo.Name != nameof(SnapshotsController.EventsAsync))
            return;

        var status = await context.GetOrCreateSchemaAsync(typeof(SnapshotProcessingResponse), null, cancellationToken);
        var error = await context.GetOrCreateSchemaAsync(typeof(SnapshotProcessingErrorResponse), null, cancellationToken);
        operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        operation.Extensions["x-sse-events"] = new EventSchemas(status, error);

        var mediaType = operation.Responses!["200"].Content!["text/event-stream"];
        mediaType.Examples = new Dictionary<string, IOpenApiExample>
        {
            ["status"] = new OpenApiExample
            {
                Summary = "An unnamed processing status event",
                Value = JsonValue.Create("data: {\"id\":\"e6c16b37-4e32-4ab4-93d5-642a88c7132f\",\"status\":\"COMPLETED\",\"progressStep\":\"FINISHED\",\"queuePosition\":null,\"canCancel\":false,\"cancelRequested\":false,\"startedAt\":null,\"moderationStatus\":\"PENDING\"}\n\n")
            },
            ["error"] = new OpenApiExample
            {
                Summary = "A named stream error event",
                Value = JsonValue.Create("event: error\ndata: {\"status\":503,\"title\":\"Processing service unavailable\"}\n\n")
            }
        };
    }

    private sealed class EventSchemas(IOpenApiSchema status, IOpenApiSchema error) : IOpenApiExtension
    {
        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("message");
            status.SerializeAsV31(writer);
            writer.WritePropertyName("error");
            error.SerializeAsV31(writer);
            writer.WriteEndObject();
        }
    }
}
