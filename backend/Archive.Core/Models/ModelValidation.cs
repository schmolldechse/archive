using System.ComponentModel.DataAnnotations;

namespace Archive.Core.Models;

public static class ModelValidation
{
    public static IReadOnlyList<FieldValidationError> GetErrors(IValidatableObject model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(field => new FieldValidationError(field, result.ErrorMessage ?? "The value is invalid.")))
            .ToArray();
    }
}
