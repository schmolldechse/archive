using System.ComponentModel.DataAnnotations;
using Archive.Core.Models.Snapshots;

namespace Archive.Api.Contracts.Snapshots.Creation;

internal static class SnapshotRequestValidation
{
    public static IEnumerable<ValidationResult> Validate(string sourceUrl, string title, string? description, string[]? tags)
    {
        var request = new SnapshotCreationRequest(sourceUrl, title, description, tags);
        foreach (var error in SnapshotValidation.GetErrors(request))
            yield return new(error.Message, [error.Field == "originUrl" ? "sourceUrl" : error.Field]);
    }
}
