using Archive.Core.Models.Snapshots;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class SnapshotSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Properties?.TryGetValue("tags", out var tags) == true && tags.Items is OpenApiSchema item)
            item.MaxLength = SnapshotValidation.MaxTagLength;
        if ((string.Equals(context.JsonPropertyInfo?.Name, "tags", StringComparison.OrdinalIgnoreCase)
             || context.ParameterDescription?.Name == "tags") && schema.Items is OpenApiSchema parameterItem)
            parameterItem.MaxLength = SnapshotValidation.MaxTagLength;
        return Task.CompletedTask;
    }
}
