namespace Archive.Core.Models.Snapshots;

public sealed record SnapshotCreationRequest(string OriginUrl, string Title, string? Description,
    IReadOnlyList<string>? Tags, Guid? CreatedByUserId = null);
public sealed record SnapshotUpload(string FileName, long Length, Func<Stream> OpenReadStream);

public abstract record SnapshotCreationResult
{
    private SnapshotCreationResult() { }
    public sealed record Created(Guid Id) : SnapshotCreationResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : SnapshotCreationResult;
    public sealed record UploadTooLarge : SnapshotCreationResult;
}
