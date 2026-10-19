using Archive.Api.Controllers.Admin;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Archive.Api.OpenApi;

/// <summary>Documents conditional access writes and their response validator.</summary>
public sealed class UserAccessOperationTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action ||
            action.ControllerTypeInfo.AsType() != typeof(UsersController) ||
            action.MethodInfo.Name is not (nameof(UsersController.Get) or nameof(UsersController.Update))) return Task.CompletedTask;

        if (operation.Responses!["200"] is not OpenApiResponse response) return Task.CompletedTask;
        response.Headers = new Dictionary<string, IOpenApiHeader>
        {
            ["ETag"] = new OpenApiHeader { Description = "Opaque strong validator for these saved access settings.", Schema = new OpenApiSchema { Type = JsonSchemaType.String } }
        };
        var condition = operation.Parameters?.FirstOrDefault(parameter => parameter.In == ParameterLocation.Header && parameter.Name == "If-Match");
        if (condition is OpenApiParameter parameter) parameter.Required = true;
        return Task.CompletedTask;
    }
}
