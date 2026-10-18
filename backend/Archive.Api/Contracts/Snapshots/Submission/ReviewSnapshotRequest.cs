using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Archive.Core.Entities;

namespace Archive.Api.Contracts.Snapshots.Submission;

/// <summary>Manual approval or rejection; the reason is optional for either decision.</summary>
public sealed class ReviewSnapshotRequest : IValidatableObject
{
    [JsonPropertyName("decision"), Required]
    [Description("Only APPROVED and REJECTED are accepted.")]
    public required SnapshotModerationStatus Decision { get; init; }

    [JsonPropertyName("reason"), StringLength(2048)]
    [Description("Optional plain-text reason, visible to the owner and moderators.")]
    public string? Reason { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Decision is not (SnapshotModerationStatus.Approved or SnapshotModerationStatus.Rejected))
            yield return new("Choose APPROVED or REJECTED.", [nameof(Decision)]);
    }
}
