using System.Reflection;
using Archive.Api.Contracts.Snapshots;
using Archive.Api.Contracts.Snapshots.Search;
using Archive.Api.Contracts.Snapshots.Creation;
using Archive.Core.Models.Snapshots;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

public sealed class SnapshotSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (IsSnapshotContract(context.JsonTypeInfo.Type) &&
            schema.Properties?.TryGetValue("tags", out var tags) == true && tags.Items is OpenApiSchema item)
            item.MaxLength = SnapshotValidation.MaxTagLength;

        var isTagProperty = context.JsonPropertyInfo is { Name: "tags", AttributeProvider: PropertyInfo property } &&
            IsSnapshotContract(property.DeclaringType);
        var isTagParameter = context.ParameterDescription is { Name: "tags" } parameter &&
            parameter.ModelMetadata?.ContainerType == typeof(SnapshotSearchRequest);
        if ((isTagProperty || isTagParameter) && schema.Items is OpenApiSchema parameterItem)
            parameterItem.MaxLength = SnapshotValidation.MaxTagLength;
        return Task.CompletedTask;
    }

    private static bool IsSnapshotContract(Type? type) => type == typeof(CreateSnapshotUrlRequest) ||
        type == typeof(CreateSnapshotFileRequest) || type == typeof(SnapshotResponse) ||
        type == typeof(SnapshotSearchRequest);
}
