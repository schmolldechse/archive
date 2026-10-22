using Archive.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

/// <summary>Documents validators used by private snapshot reads and conditional decisions.</summary>
public sealed class SnapshotSubmissionOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            !action.ControllerTypeInfo.IsSubclassOf(typeof(SnapshotSubmissionControllerBase))) return Task.CompletedTask;
        if (action.MethodInfo.Name is "Get" or "Review" or "Resubmit" or "Update" &&
            operation.Responses?["200"] is OpenApiResponse response)
            response.Headers = new Dictionary<string, IOpenApiHeader>
            {
                ["ETag"] = new OpenApiHeader
                {
                    Description = "Opaque strong validator scoped to this snapshot representation.",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            };
        var match = operation.Parameters?.FirstOrDefault(p => p.In == ParameterLocation.Header && p.Name == "If-Match");
        if (match is OpenApiParameter parameter) parameter.Required = true;
        return Task.CompletedTask;
    }
}
