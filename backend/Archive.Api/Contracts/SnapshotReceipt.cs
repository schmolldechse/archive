using System.ComponentModel.DataAnnotations;

namespace Archive.Api.Contracts;

public sealed record SnapshotReceipt([property: Required] Guid Id,
    [property: Required] string StatusUrl, [property: Required] string SnapshotUrl);
