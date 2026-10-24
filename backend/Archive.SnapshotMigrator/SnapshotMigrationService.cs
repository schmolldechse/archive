using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Options;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;

namespace Archive.SnapshotMigrator;

/// <summary>Migrates stored snapshots to the current manifest-backed storage contract.</summary>
internal sealed class SnapshotMigrationService(
    DataContext database,
    IArchiveObjectStore objectStore,
    ArchiveOptions archiveOptions)
{
    private const string JournalPrefix = "snapshot-migrations/manifest-v3";
    private static readonly JsonSerializerOptions ManifestJson = new() { WriteIndented = true };
    private static readonly Regex ExecutableHtml = new("<script\\b|\\son[a-z]+\\s*=",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public async Task<int> ExecuteAsync(SnapshotMigratorOptions options, CancellationToken cancellationToken)
    {
        if (options.Action == SnapshotMigratorAction.Cleanup && options.All)
        {
            var assessments = await AssessAllAsync(cancellationToken);
            var incomplete = assessments.Where(item => item.State != SnapshotMigrationState.Current).ToArray();
            if (incomplete.Length != 0)
            {
                WriteAssessments(incomplete, options.Json);
                Console.Error.WriteLine("Cleanup refused: not every stored snapshot has a valid current manifest.");
                return 1;
            }
        }

        if (options.Action == SnapshotMigratorAction.DryRun)
        {
            var assessments = options.All
                ? await AssessAllAsync(cancellationToken)
                : [await AssessAsync(options.SnapshotId!.Value, cancellationToken)];
            WriteAssessments(assessments, options.Json);
            return assessments.Any(item => item.State is SnapshotMigrationState.Blocked or
                SnapshotMigrationState.Inconsistent or SnapshotMigrationState.Missing or SnapshotMigrationState.Pending)
                ? 1 : 0;
        }

        var snapshotIds = options.All
            ? options.Action == SnapshotMigratorAction.Update
                ? await StoredSnapshotIdsAsync(cancellationToken)
                : await JournalSnapshotIdsAsync(cancellationToken)
            : [options.SnapshotId!.Value];
        var results = new List<SnapshotMigrationResult>();
        foreach (var snapshotId in snapshotIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = options.Action switch
            {
                SnapshotMigratorAction.Update => await UpdateAsync(snapshotId, cancellationToken),
                SnapshotMigratorAction.Restore => await RestoreAsync(snapshotId, cancellationToken),
                SnapshotMigratorAction.Cleanup => await CleanupAsync(snapshotId, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported snapshot migration action.")
            };
            results.Add(result);
            if (!options.Json)
                Console.WriteLine($"{result.SnapshotId}  {result.Action,-8}  {(result.Succeeded ? "ok" : "failed"),-6}  {result.Message}");
        }
        if (options.Json)
            Console.WriteLine(JsonSerializer.Serialize(results.ToArray(), SnapshotMigrationJsonContext.Default.SnapshotMigrationResultArray));
        return results.All(result => result.Succeeded) ? 0 : 1;
    }

    private async Task<SnapshotMigrationAssessment[]> AssessAllAsync(CancellationToken cancellationToken)
    {
        var storedIds = await StoredSnapshotIdsAsync(cancellationToken);
        var journalIds = await JournalSnapshotIdsAsync(cancellationToken);
        var ids = storedIds.Concat(journalIds).Distinct().Order().ToArray();
        var assessments = new List<SnapshotMigrationAssessment>(ids.Length);
        foreach (var id in ids)
            assessments.Add(await AssessAsync(id, cancellationToken));
        return assessments.ToArray();
    }

    private async Task<IReadOnlyList<Guid>> StoredSnapshotIdsAsync(CancellationToken cancellationToken) =>
        await database.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.Quality != null && snapshot.ContentPrefix != null)
            .OrderBy(snapshot => snapshot.CreatedAt).Select(snapshot => snapshot.Id).ToArrayAsync(cancellationToken);

    private async Task<IReadOnlyList<Guid>> JournalSnapshotIdsAsync(CancellationToken cancellationToken)
    {
        var entries = await objectStore.ListAsync(JournalPrefix + "/", cancellationToken);
        return entries.Select(item => Path.GetFileNameWithoutExtension(item.Key))
            .Select(value => Guid.TryParseExact(value, "N", out var id) ? id : (Guid?)null)
            .Where(id => id is not null).Select(id => id!.Value).Distinct().Order().ToArray();
    }

    private async Task<SnapshotMigrationAssessment> AssessAsync(Guid snapshotId,
        CancellationToken cancellationToken)
    {
        var snapshot = await database.Snapshots.AsNoTracking().SingleOrDefaultAsync(item => item.Id == snapshotId,
            cancellationToken);
        if (snapshot?.ContentPrefix is null || snapshot.Quality is null)
            return new(snapshotId, SnapshotMigrationState.Missing,
                "The snapshot has no stored content generation.", snapshot?.ContentPrefix, snapshot?.ReplayVersion);

        var journal = await ReadJournalAsync(snapshotId, cancellationToken);
        if (journal is not null && snapshot.ContentPrefix == journal.OriginalContentPrefix)
        {
            var state = journal.Status == SnapshotMigrationJournal.Restored
                ? SnapshotMigrationState.Restored : SnapshotMigrationState.Pending;
            return new(snapshotId, state,
                state == SnapshotMigrationState.Restored
                    ? "The original generation is active; cleanup removes the migrated generation."
                    : "A prepared generation exists but was not activated; cleanup is required before retrying.",
                snapshot.ContentPrefix, snapshot.ReplayVersion);
        }
        if (journal is not null && snapshot.ContentPrefix != journal.MigratedContentPrefix)
            return new(snapshotId, SnapshotMigrationState.Inconsistent,
                "The active generation matches neither side of the migration journal.", snapshot.ContentPrefix,
                snapshot.ReplayVersion);

        var manifestObject = await objectStore.GetAsync($"{snapshot.ContentPrefix}/manifest.json", cancellationToken);
        if (manifestObject is not null)
        {
            SnapshotReplayManifest? manifest = null;
            try
            {
                await using (manifestObject.Content)
                    manifest = await JsonSerializer.DeserializeAsync<SnapshotReplayManifest>(manifestObject.Content,
                        cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                // Older manifests have a different shape and are assessed from their stored objects below.
            }

            if (manifest?.Version == SnapshotReplayManifest.CurrentManifestVersion)
            {
                var error = await ValidateCurrentManifestAsync(snapshot.ContentPrefix, snapshot.ReplayVersion, manifest,
                    cancellationToken);
                return error is null
                    ? new(snapshotId, SnapshotMigrationState.Current,
                        journal is null ? "Current snapshot manifest is valid." : "Migrated generation is active and valid.",
                        snapshot.ContentPrefix, snapshot.ReplayVersion)
                    : new(snapshotId, SnapshotMigrationState.Inconsistent, error, snapshot.ContentPrefix,
                        snapshot.ReplayVersion);
            }
        }

        if (snapshot.ReplayVersion is not null)
            return new(snapshotId, SnapshotMigrationState.Inconsistent,
                "The database marks the snapshot as interactive but no valid current manifest exists.",
                snapshot.ContentPrefix, snapshot.ReplayVersion);

        var objects = await ListGenerationAsync(snapshot.ContentPrefix, cancellationToken);
        var indexKey = $"{snapshot.ContentPrefix}/index.html";
        var screenshotKey = $"{snapshot.ContentPrefix}/screenshot.png";
        if (objects.All(item => item.Key != indexKey))
            return new(snapshotId, SnapshotMigrationState.Blocked, "index.html is missing.", snapshot.ContentPrefix,
                snapshot.ReplayVersion);
        if (objects.All(item => item.Key != screenshotKey))
            return new(snapshotId, SnapshotMigrationState.Blocked, "screenshot.png is missing.", snapshot.ContentPrefix,
                snapshot.ReplayVersion);
        if (objects.Any(item => !TryRelativePath(snapshot.ContentPrefix, item.Key, out _)))
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation contains an unsafe object path.",
                snapshot.ContentPrefix, snapshot.ReplayVersion);
        if (objects.Count(item => TryRelativePath(snapshot.ContentPrefix, item.Key, out var path) &&
                IsDisplayResource(path)) > archiveOptions.MaxResources + 1)
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation exceeds the resource limit.",
                snapshot.ContentPrefix, snapshot.ReplayVersion);
        if (objects.Sum(item => item.Length) > archiveOptions.MaxStorageBytes)
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation exceeds the storage limit.",
                snapshot.ContentPrefix, snapshot.ReplayVersion);

        return new(snapshotId, SnapshotMigrationState.MigratableStatic,
            "The snapshot can receive a current manifest; script execution remains disabled because its original capture contract is not verifiable.",
            snapshot.ContentPrefix, snapshot.ReplayVersion);
    }

    private async Task<string?> ValidateCurrentManifestAsync(string contentPrefix, int? replayVersion,
        SnapshotReplayManifest manifest, CancellationToken cancellationToken)
    {
        if (manifest.Replay.Version != replayVersion)
            return "The database replay version does not match the snapshot manifest.";
        if (manifest.Resources.Count == 0 || manifest.Resources.Count > archiveOptions.MaxResources + 1)
            return "The snapshot manifest contains an invalid resource count.";
        if (manifest.Resources.Count(resource => resource.Path == "index.html" &&
                resource.Role == SnapshotReplayResourceRoles.RootDocument) != 1)
            return "The snapshot manifest does not define exactly one root document.";
        var root = manifest.Resources.Single(resource => resource.Path == "index.html");
        if (!string.Equals(root.Sha256, manifest.Provenance.IndexSha256, StringComparison.OrdinalIgnoreCase))
            return "The root-document hash does not match the snapshot provenance.";
        if (manifest.Resources.Select(resource => resource.Path).Distinct(StringComparer.Ordinal).Count() !=
            manifest.Resources.Count)
            return "The snapshot manifest contains duplicate resource paths.";

        foreach (var resource in manifest.Resources)
        {
            if (!IsSafeRelativePath(resource.Path) ||
                !string.Equals(resource.ObjectKey, $"{contentPrefix}/{resource.Path}", StringComparison.Ordinal) ||
                resource.Role is not (SnapshotReplayResourceRoles.RootDocument or
                    SnapshotReplayResourceRoles.NestedDocument or SnapshotReplayResourceRoles.Asset))
                return $"The snapshot manifest contains an unsafe resource: {resource.Path}";
            var stored = await objectStore.GetAsync(resource.ObjectKey, cancellationToken);
            if (stored is null) return $"The snapshot resource is missing: {resource.Path}";
            await using (stored.Content)
            {
                var hash = await HashAsync(stored.Content, cancellationToken);
                if (!string.Equals(hash, resource.Sha256, StringComparison.OrdinalIgnoreCase))
                    return $"The snapshot resource hash is invalid: {resource.Path}";
            }
        }

        if (!manifest.Provenance.SourceObjectKey.StartsWith(contentPrefix + "/", StringComparison.Ordinal))
            return "The snapshot source object is outside the active generation.";
        var source = await objectStore.GetAsync(manifest.Provenance.SourceObjectKey, cancellationToken);
        if (source is null) return "The snapshot source object is missing.";
        await using (source.Content)
        {
            if (!string.Equals(await HashAsync(source.Content, cancellationToken), manifest.Provenance.SourceSha256,
                    StringComparison.OrdinalIgnoreCase))
                return "The snapshot source object hash is invalid.";
        }
        if (await objectStore.GetAsync($"{contentPrefix}/screenshot.png", cancellationToken) is not { } screenshot)
            return "The snapshot screenshot is missing.";
        await screenshot.Content.DisposeAsync();
        return null;
    }

    private async Task<SnapshotMigrationResult> UpdateAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var assessment = await AssessAsync(snapshotId, cancellationToken);
        if (assessment.State == SnapshotMigrationState.Current)
            return new(snapshotId, "update", true, "Already current; no changes were made.");
        if (assessment.State != SnapshotMigrationState.MigratableStatic)
            return new(snapshotId, "update", false, assessment.Message);

        var snapshot = await database.Snapshots.AsNoTracking().SingleAsync(item => item.Id == snapshotId,
            cancellationToken);
        var originalPrefix = snapshot.ContentPrefix!;
        var originalObjects = await ListGenerationAsync(originalPrefix, cancellationToken);
        var legacy = await ReadLegacyMetadataAsync(originalPrefix, cancellationToken);
        var migrationId = Guid.NewGuid();
        var migratedPrefix = $"snapshots/{snapshotId:N}/generations/manifest-v3/{migrationId:N}";
        var migratedKeys = new List<string>();
        var journalPrepared = false;
        var activated = false;

        try
        {
            foreach (var item in originalObjects)
            {
                if (!TryRelativePath(originalPrefix, item.Key, out var relativePath) || relativePath == "manifest.json")
                    continue;
                var destination = $"{migratedPrefix}/{relativePath}";
                await objectStore.CopyAsync(item.Key, destination, cancellationToken);
                migratedKeys.Add(destination);
            }

            var resourceCandidates = originalObjects.Where(item =>
                TryRelativePath(originalPrefix, item.Key, out var relativePath) && IsDisplayResource(relativePath))
                .ToArray();
            var resources = new List<SnapshotReplayResource>(resourceCandidates.Length);
            string? indexHash = null;
            byte[]? indexBytes = null;
            foreach (var item in resourceCandidates)
            {
                TryRelativePath(originalPrefix, item.Key, out var relativePath);
                var stored = await objectStore.GetAsync(item.Key, cancellationToken)
                    ?? throw new InvalidDataException($"Snapshot object disappeared during migration: {item.Key}");
                byte[] bytes;
                await using (stored.Content)
                    bytes = await ReadBytesAsync(stored.Content, archiveOptions.MaxStorageBytes, cancellationToken);
                var hash = Hash(bytes);
                if (relativePath == "index.html")
                {
                    indexHash = hash;
                    indexBytes = bytes;
                }
                legacy.Resources.TryGetValue(item.Key, out var previous);
                var contentType = string.IsNullOrWhiteSpace(previous?.ContentType)
                    ? stored.ContentType : previous.ContentType;
                resources.Add(new()
                {
                    Path = relativePath,
                    SourceUrl = relativePath == "index.html" ? snapshot.OriginUrl : previous?.SourceUrl,
                    ContentType = contentType ?? "application/octet-stream",
                    ObjectKey = $"{migratedPrefix}/{relativePath}",
                    Sha256 = hash,
                    Role = SnapshotReplayResourceRoles.Infer(relativePath, contentType ?? stored.ContentType)
                });
            }
            if (indexHash is null || indexBytes is null)
                throw new InvalidDataException("index.html could not be read.");

            var sourceItem = originalObjects.FirstOrDefault(item =>
                TryRelativePath(originalPrefix, item.Key, out var relativePath) && IsSource(relativePath));
            string sourceKey;
            string sourceHash;
            string sourceType;
            long additionalSourceBytes = 0;
            if (sourceItem is null)
            {
                sourceKey = $"{migratedPrefix}/source.legacy.html";
                await objectStore.CopyAsync($"{originalPrefix}/index.html", sourceKey, cancellationToken);
                migratedKeys.Add(sourceKey);
                sourceHash = indexHash;
                sourceType = "text/html; charset=utf-8";
                additionalSourceBytes = indexBytes.LongLength;
            }
            else
            {
                TryRelativePath(originalPrefix, sourceItem.Key, out var sourcePath);
                sourceKey = $"{migratedPrefix}/{sourcePath}";
                var source = await objectStore.GetAsync(sourceItem.Key, cancellationToken)
                    ?? throw new InvalidDataException("The snapshot source object disappeared during migration.");
                await using (source.Content)
                {
                    sourceHash = await HashAsync(source.Content, cancellationToken);
                    sourceType = source.ContentType;
                }
            }

            var manifest = new SnapshotReplayManifest
            {
                Version = SnapshotReplayManifest.CurrentManifestVersion,
                Provenance = new()
                {
                    DerivedDisplayDocument = true,
                    SourceObjectKey = sourceKey,
                    SourceSha256 = sourceHash,
                    IndexSha256 = indexHash,
                    SourceUrl = snapshot.OriginUrl,
                    SourceMimeType = sourceType,
                    CapturedAt = snapshot.CaptureCompletedAt ?? snapshot.CreatedAt,
                    DocumentState = "legacy-derived",
                    RedirectChain = []
                },
                Replay = new()
                {
                    Version = null,
                    Normalizer = "snapshot-migrator-v1",
                    ContainsExecutableContent = ExecutableHtml.IsMatch(Encoding.UTF8.GetString(indexBytes)),
                    Diagnostic = "Migrated from a legacy capture contract; script execution remains disabled."
                },
                Resources = resources,
                MissingUrls = legacy.MissingUrls,
                MissingUrlCount = legacy.MissingUrlCount,
                Incomplete = legacy.Incomplete
            };
            var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJson);
            var manifestKey = $"{migratedPrefix}/manifest.json";
            await PutAsync(manifestKey, manifestBytes, "application/json", cancellationToken);
            migratedKeys.Add(manifestKey);

            var validationError = await ValidateCurrentManifestAsync(migratedPrefix, null, manifest,
                cancellationToken);
            if (validationError is not null)
                throw new InvalidDataException($"The prepared snapshot generation is invalid: {validationError}");

            var migratedStorageBytes = originalObjects.Where(item => !item.Key.EndsWith("/manifest.json", StringComparison.Ordinal))
                .Sum(item => item.Length) + additionalSourceBytes + manifestBytes.LongLength;
            var journal = new SnapshotMigrationJournal
            {
                SnapshotId = snapshotId,
                MigrationId = migrationId,
                Status = SnapshotMigrationJournal.Prepared,
                OriginalContentPrefix = originalPrefix,
                MigratedContentPrefix = migratedPrefix,
                OriginalReplayVersion = snapshot.ReplayVersion,
                MigratedReplayVersion = null,
                OriginalStorageBytes = snapshot.StorageBytes,
                MigratedStorageBytes = migratedStorageBytes,
                OriginalResourceCount = snapshot.ResourceCount,
                MigratedResourceCount = Math.Max(0, resources.Count - 1),
                OriginalObjectKeys = originalObjects.Select(item => item.Key).ToArray(),
                MigratedObjectKeys = migratedKeys.ToArray(),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await WriteJournalAsync(journal, cancellationToken);
            journalPrepared = true;

            var updated = await database.Snapshots.Where(item => item.Id == snapshotId &&
                    item.ContentPrefix == originalPrefix && item.ReplayVersion == snapshot.ReplayVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.ContentPrefix, migratedPrefix)
                    .SetProperty(item => item.ReplayVersion, (int?)null)
                    .SetProperty(item => item.StorageBytes, migratedStorageBytes)
                    .SetProperty(item => item.ResourceCount, Math.Max(0, resources.Count - 1)), cancellationToken);
            if (updated != 1)
                return new(snapshotId, "update", false,
                    "The snapshot changed before activation; the prepared generation can be removed with cleanup.");
            activated = true;

            await WriteJournalAsync(journal with
            {
                Status = SnapshotMigrationJournal.Applied,
                AppliedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
            return new(snapshotId, "update", true, "Activated a validated manifest-backed snapshot generation.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (!activated && !journalPrepared)
            {
                foreach (var key in migratedKeys.AsEnumerable().Reverse())
                {
                    try { await objectStore.DeleteAsync(key, CancellationToken.None); }
                    catch { /* The unique inactive generation remains safe to inspect or remove manually. */ }
                }
            }
            return new(snapshotId, "update", false,
                activated
                    ? $"The new generation is active, but journal finalization failed: {exception.Message}"
                    : $"Migration stopped without changing the active generation: {exception.Message}");
        }
    }

    private async Task<SnapshotMigrationResult> RestoreAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var journal = await ReadJournalAsync(snapshotId, cancellationToken);
        if (journal is null) return new(snapshotId, "restore", false, "No migration journal exists.");
        var snapshot = await database.Snapshots.AsNoTracking().SingleOrDefaultAsync(item => item.Id == snapshotId,
            cancellationToken);
        if (snapshot is null) return new(snapshotId, "restore", false, "Snapshot not found.");
        if (snapshot.ContentPrefix == journal.OriginalContentPrefix)
            return new(snapshotId, "restore", true, "The original generation is already active.");
        if (snapshot.ContentPrefix != journal.MigratedContentPrefix)
            return new(snapshotId, "restore", false, "The active generation does not match the migration journal.");

        var originalKeys = (await objectStore.ListAsync(journal.OriginalContentPrefix + "/", cancellationToken))
            .Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        if (journal.OriginalObjectKeys.Any(key => !originalKeys.Contains(key)))
            return new(snapshotId, "restore", false, "At least one original snapshot object is missing.");

        var updated = await database.Snapshots.Where(item => item.Id == snapshotId &&
                item.ContentPrefix == journal.MigratedContentPrefix)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.ContentPrefix, journal.OriginalContentPrefix)
                .SetProperty(item => item.ReplayVersion, journal.OriginalReplayVersion)
                .SetProperty(item => item.StorageBytes, journal.OriginalStorageBytes)
                .SetProperty(item => item.ResourceCount, journal.OriginalResourceCount), cancellationToken);
        if (updated != 1) return new(snapshotId, "restore", false, "The snapshot changed before restore.");
        await WriteJournalAsync(journal with
        {
            Status = SnapshotMigrationJournal.Restored,
            RestoredAt = DateTimeOffset.UtcNow
        }, cancellationToken);
        return new(snapshotId, "restore", true, "The original snapshot generation is active again.");
    }

    private async Task<SnapshotMigrationResult> CleanupAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var journal = await ReadJournalAsync(snapshotId, cancellationToken);
        if (journal is null) return new(snapshotId, "cleanup", true, "No migration data exists.");
        var prefix = await database.Snapshots.AsNoTracking().Where(item => item.Id == snapshotId)
            .Select(item => item.ContentPrefix).SingleOrDefaultAsync(cancellationToken);
        IReadOnlyList<string> inactiveKeys;
        if (prefix == journal.MigratedContentPrefix)
        {
            var assessment = await AssessAsync(snapshotId, cancellationToken);
            if (assessment.State != SnapshotMigrationState.Current)
                return new(snapshotId, "cleanup", false,
                    "The migrated generation is not valid; cleanup would remove the restore source.");
            inactiveKeys = journal.OriginalObjectKeys;
        }
        else if (prefix == journal.OriginalContentPrefix)
            inactiveKeys = journal.MigratedObjectKeys;
        else
            return new(snapshotId, "cleanup", false, "The active generation does not match the migration journal.");

        foreach (var key in inactiveKeys.OrderByDescending(key => key.Length))
            await objectStore.DeleteAsync(key, cancellationToken);
        await objectStore.DeleteAsync(JournalKey(snapshotId), cancellationToken);
        return new(snapshotId, "cleanup", true,
            prefix == journal.MigratedContentPrefix
                ? "Removed the original generation and migration journal; restore is no longer possible."
                : "Removed the inactive migrated generation and migration journal.");
    }

    private async Task<SnapshotMigrationJournal?> ReadJournalAsync(Guid snapshotId,
        CancellationToken cancellationToken)
    {
        var stored = await objectStore.GetAsync(JournalKey(snapshotId), cancellationToken);
        if (stored is null) return null;
        await using (stored.Content)
            return await JsonSerializer.DeserializeAsync(stored.Content,
                SnapshotMigrationJsonContext.Default.SnapshotMigrationJournal, cancellationToken);
    }

    private Task WriteJournalAsync(SnapshotMigrationJournal journal, CancellationToken cancellationToken) =>
        PutAsync(JournalKey(journal.SnapshotId),
            JsonSerializer.SerializeToUtf8Bytes(journal, SnapshotMigrationJsonContext.Default.SnapshotMigrationJournal),
            "application/json", cancellationToken);

    private async Task<LegacySnapshotMetadata> ReadLegacyMetadataAsync(string prefix,
        CancellationToken cancellationToken)
    {
        var stored = await objectStore.GetAsync($"{prefix}/manifest.json", cancellationToken);
        if (stored is null) return new(new Dictionary<string, LegacySnapshotResource>(), [], 0, false);
        try
        {
            await using (stored.Content)
            using (var document = await JsonDocument.ParseAsync(stored.Content, cancellationToken: cancellationToken))
            {
                var root = document.RootElement;
                var resources = new Dictionary<string, LegacySnapshotResource>(StringComparer.Ordinal);
                if (root.TryGetProperty("resources", out var resourceArray) && resourceArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var resource in resourceArray.EnumerateArray())
                    {
                        if (!resource.TryGetProperty("objectKey", out var objectKeyElement) ||
                            objectKeyElement.GetString() is not { } objectKey)
                            continue;
                        resources[objectKey] = new(
                            resource.TryGetProperty("url", out var url) ? url.GetString() : null,
                            resource.TryGetProperty("contentType", out var type) ? type.GetString() : null);
                    }
                }
                var missingUrls = root.TryGetProperty("missingUrls", out var missing) && missing.ValueKind == JsonValueKind.Array
                    ? missing.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null)
                        .Select(item => item!).ToArray()
                    : [];
                var missingCount = root.TryGetProperty("missingUrlCount", out var count) && count.TryGetInt32(out var value)
                    ? value : missingUrls.Length;
                var incomplete = root.TryGetProperty("incomplete", out var incompleteElement) &&
                    incompleteElement.ValueKind == JsonValueKind.True;
                return new(resources, missingUrls, missingCount, incomplete);
            }
        }
        catch (JsonException)
        {
            return new(new Dictionary<string, LegacySnapshotResource>(), [], 0, true);
        }
    }

    private async Task<IReadOnlyList<ArchiveObjectInfo>> ListGenerationAsync(string prefix,
        CancellationToken cancellationToken)
    {
        var objects = await objectStore.ListAsync(prefix.TrimEnd('/') + "/", cancellationToken);
        return objects.Where(item => !item.Key.StartsWith(prefix.TrimEnd('/') + "/generations/", StringComparison.Ordinal))
            .OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
    }

    private static bool TryRelativePath(string prefix, string key, out string relativePath)
    {
        var start = prefix.TrimEnd('/') + "/";
        relativePath = key.StartsWith(start, StringComparison.Ordinal) ? key[start.Length..] : string.Empty;
        return IsSafeRelativePath(relativePath);
    }

    private static bool IsSafeRelativePath(string path) =>
        !string.IsNullOrWhiteSpace(path) && path[0] != '/' && path[0] != '\\' &&
        !path.Contains('\\') && !path.Any(char.IsControl) &&
        path.Replace('\\', '/').Split('/').All(segment => segment is not ("" or "." or ".."));

    private static bool IsDisplayResource(string path) =>
        path != "manifest.json" && path != "screenshot.png" && !IsSource(path);

    private static bool IsSource(string path) =>
        !path.Contains('/') && path.StartsWith("source.", StringComparison.OrdinalIgnoreCase);

    private static string JournalKey(Guid snapshotId) => $"{JournalPrefix}/{snapshotId:N}.json";

    private async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(bytes, writable: false);
        await objectStore.PutAsync(key, stream, contentType, cancellationToken);
    }

    private static async Task<byte[]> ReadBytesAsync(Stream stream, long maximumBytes,
        CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var block = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(block, cancellationToken)) != 0)
        {
            if (buffer.Length + read > maximumBytes)
                throw new InvalidDataException("A snapshot object exceeds the configured storage limit.");
            await buffer.WriteAsync(block.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    private static async Task<string> HashAsync(Stream stream, CancellationToken cancellationToken) =>
        Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();

    private static string Hash(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void WriteAssessments(IReadOnlyList<SnapshotMigrationAssessment> assessments, bool json)
    {
        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(assessments.ToArray(),
                SnapshotMigrationJsonContext.Default.SnapshotMigrationAssessmentArray));
            return;
        }
        foreach (var item in assessments)
            Console.WriteLine($"{item.SnapshotId}  {item.State,-18}  {item.Message}");
        var groups = assessments.GroupBy(item => item.State).OrderBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}");
        Console.WriteLine("Summary: " + string.Join(", ", groups));
    }
}
