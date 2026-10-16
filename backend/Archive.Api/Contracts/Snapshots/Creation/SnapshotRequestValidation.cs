using System.ComponentModel.DataAnnotations;
using Archive.Core.Models.Snapshots;
using Archive.Core.Models;
using Archive.Api.Validation;

namespace Archive.Api.Contracts.Snapshots.Creation;

internal static class SnapshotRequestValidation
{
    public static IEnumerable<ValidationResult> Validate(string sourceUrl, string title, string? description, string[]? tags)
    {
        var request = new SnapshotCreationRequest(sourceUrl, title, description, tags);
        return ModelValidation.GetErrors(request).ToValidationResults();
    }
}
