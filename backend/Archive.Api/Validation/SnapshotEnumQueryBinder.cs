using System.Reflection;
using System.Text.Json.Serialization;
using Archive.Core.Models.Snapshots;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Archive.Api.Validation;

public sealed class SnapshotEnumQueryBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (values == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, values);
        var type = bindingContext.ModelMetadata.UnderlyingOrModelType;
        var value = values.FirstValue;
        var canonical = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(field => string.Equals(
                field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name, value, StringComparison.Ordinal));
        if (type == typeof(SnapshotSortOrder))
        {
            if (values.Length == 1 && canonical is not null)
                bindingContext.Result = ModelBindingResult.Success(canonical.GetValue(null));
            else
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Capture order must be asc or desc.");
        }
        else if (string.IsNullOrWhiteSpace(value) && bindingContext.ModelMetadata.IsNullableValueType)
        {
            bindingContext.Result = ModelBindingResult.Success(null);
        }
        else
        {
            if (canonical is not null)
                bindingContext.Result = ModelBindingResult.Success(canonical.GetValue(null));
            else if (Enum.TryParse(type, value, ignoreCase: true, out var parsed) && Enum.IsDefined(type, parsed))
                bindingContext.Result = ModelBindingResult.Success(parsed);
            else
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Choose a valid value.");
        }
        return Task.CompletedTask;
    }
}
