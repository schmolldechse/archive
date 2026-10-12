namespace Archive.Core.Models.Archive;

public abstract record ArchiveCreationResult
{
    private ArchiveCreationResult() { }
    public sealed record Created(ArchiveStatus Archive) : ArchiveCreationResult;
    public sealed record Invalid(IReadOnlyList<FieldValidationError> Errors) : ArchiveCreationResult;
    public sealed record UploadTooLarge : ArchiveCreationResult;
}
