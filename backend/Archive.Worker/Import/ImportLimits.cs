namespace Archive.Worker.Import;

public sealed record ImportLimits(long MaxBytes, int MaxResources, int MaxDepth, int MaxObjects);
