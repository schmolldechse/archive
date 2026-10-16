using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Archive.Core.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Archive.Api.Validation;

internal static class ValidationErrors
{
    public static IEnumerable<ValidationResult> ToValidationResults(this IEnumerable<FieldValidationError> errors) =>
        errors.Select(error => new ValidationResult(error.Message,
            error.Field.Length == 0 ? [] : [JsonNamingPolicy.CamelCase.ConvertName(error.Field)]));

    public static void AddErrors(this ModelStateDictionary modelState, IEnumerable<FieldValidationError> errors)
    {
        foreach (var error in errors)
        {
            var field = JsonNamingPolicy.CamelCase.ConvertName(error.Field);
            if (modelState.TryGetValue(field, out var entry) &&
                entry.Errors.Any(existing => existing.ErrorMessage == error.Message))
                continue;
            modelState.AddModelError(field, error.Message);
        }
    }
}
