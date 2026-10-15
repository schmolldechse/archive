using Archive.Core.Options;

namespace Archive.Api.Options;

public sealed class SnapshotUploadLimits(ArchiveOptions options)
{
    public const long MultipartOverheadBytes = 1_000_000;
    public long MaxFileBytes { get; } = options.MaxUploadBytes;
    public long MaxRequestBodyBytes { get; } = checked(options.MaxUploadBytes + MultipartOverheadBytes);
}
