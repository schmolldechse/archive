using System.Security.Cryptography;
using System.Text.Json;
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
    private const string JournalPrefix = "snapshot-migrations/manifest-v4";
    private static readonly JsonSerializerOptions ManifestJson = new() { WriteIndented = true };

    public async Task<int> ExecuteAsync(SnapshotMigratorOptions options, CancellationToken cancellationToken)
    {
        if (options.Action == SnapshotMigratorAction.Clean && options.All)
        {
            var assessments = await AssessAllAsync(cancellationToken);
            var incomplete = assessments.Where(item => item.State != SnapshotMigrationState.Current).ToArray();
            if (incomplete.Length != 0)
            {
                WriteAssessments(incomplete, options.Json);
                Console.Error.WriteLine("Clean refused: not every stored snapshot has a valid current manifest.");
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
            ? options.Action == SnapshotMigratorAction.Migrate
                ? await StoredSnapshotIdsAsync(cancellationToken)
                : await JournalSnapshotIdsAsync(cancellationToken)
            : [options.SnapshotId!.Value];
        var results = new List<SnapshotMigrationResult>();
        foreach (var snapshotId in snapshotIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = options.Action switch
            {
                SnapshotMigratorAction.Migrate => await MigrateAsync(snapshotId, cancellationToken),
                SnapshotMigratorAction.Restore => await RestoreAsync(snapshotId, cancellationToken),
                SnapshotMigratorAction.Clean => await CleanAsync(snapshotId, cancellationToken),
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
                "The snapshot has no stored content generation.", snapshot?.ContentPrefix);
        if (!IsSnapshotContentPrefix(snapshotId, snapshot.ContentPrefix))
            return new(snapshotId, SnapshotMigrationState.Inconsistent,
                "The active content prefix is outside the snapshot namespace.", snapshot.ContentPrefix);

        SnapshotMigrationJournal? journal;
        try { journal = await ReadJournalAsync(snapshotId, cancellationToken); }
        catch (InvalidDataException exception)
        {
            return new(snapshotId, SnapshotMigrationState.Inconsistent, exception.Message, snapshot.ContentPrefix);
        }
        if (journal is not null && snapshot.ContentPrefix == journal.OriginalContentPrefix)
        {
            var state = journal.Status == SnapshotMigrationJournal.Restored
                ? SnapshotMigrationState.Restored : SnapshotMigrationState.Pending;
            return new(snapshotId, state,
                state == SnapshotMigrationState.Restored
                    ? "The original generation is active; clean removes the migrated generation."
                    : "A prepared generation exists but was not activated; clean is required before retrying.",
                snapshot.ContentPrefix);
        }
        if (journal is not null && snapshot.ContentPrefix != journal.MigratedContentPrefix)
            return new(snapshotId, SnapshotMigrationState.Inconsistent,
                "The active generation matches neither side of the migration journal.", snapshot.ContentPrefix);

        var manifestObject = await objectStore.GetAsync($"{snapshot.ContentPrefix}/manifest.json", cancellationToken);
        if (manifestObject is not null)
        {
            SnapshotManifest? manifest = null;
            try
            {
                await using (manifestObject.Content)
                    manifest = await JsonSerializer.DeserializeAsync<SnapshotManifest>(manifestObject.Content,
                        cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                // Older manifests have a different shape and are assessed from their stored objects below.
            }

            if (manifest?.Version == SnapshotManifest.CurrentVersion)
            {
                var error = await ValidateCurrentManifestAsync(snapshot.ContentPrefix, manifest, cancellationToken);
                return error is null
                    ? new(snapshotId, SnapshotMigrationState.Current,
                        journal is null ? "Current snapshot manifest is valid." : "Migrated generation is active and valid.",
                        snapshot.ContentPrefix)
                    : new(snapshotId, SnapshotMigrationState.Inconsistent, error, snapshot.ContentPrefix);
            }
        }

        var objects = await ListGenerationAsync(snapshot.ContentPrefix, cancellationToken);
        var indexKey = $"{snapshot.ContentPrefix}/index.html";
        var screenshotKey = $"{snapshot.ContentPrefix}/screenshot.png";
        if (objects.All(item => item.Key != indexKey))
            return new(snapshotId, SnapshotMigrationState.Blocked, "index.html is missing.", snapshot.ContentPrefix);
        if (objects.All(item => item.Key != screenshotKey))
            return new(snapshotId, SnapshotMigrationState.Blocked, "screenshot.png is missing.", snapshot.ContentPrefix);
        if (objects.Any(item => !TryRelativePath(snapshot.ContentPrefix, item.Key, out _)))
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation contains an unsafe object path.",
                snapshot.ContentPrefix);
        if (objects.Count(item => TryRelativePath(snapshot.ContentPrefix, item.Key, out var path) &&
                IsDisplayResource(path)) > archiveOptions.MaxResources + 1)
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation exceeds the resource limit.",
                snapshot.ContentPrefix);
        if (!FitsStorageLimit(objects))
            return new(snapshotId, SnapshotMigrationState.Blocked, "The generation exceeds the storage limit.",
                snapshot.ContentPrefix);

        return new(snapshotId, SnapshotMigrationState.Migratable,
            "The snapshot can receive a current manifest-backed generation.", snapshot.ContentPrefix);
    }

    private async Task<string?> ValidateCurrentManifestAsync(string contentPrefix, SnapshotManifest manifest,
        CancellationToken cancellationToken)
    {
        if (manifest.Provenance is null || manifest.Interaction is null || manifest.Resources is null ||
            string.IsNullOrWhiteSpace(manifest.Interaction.Normalizer) ||
            string.IsNullOrWhiteSpace(manifest.Provenance.SourceObjectKey) ||
            string.IsNullOrWhiteSpace(manifest.Provenance.SourceSha256) ||
            string.IsNullOrWhiteSpace(manifest.Provenance.IndexSha256) ||
            string.IsNullOrWhiteSpace(manifest.Provenance.SourceMimeType) ||
            string.IsNullOrWhiteSpace(manifest.Provenance.DocumentState))
            return "The current snapshot manifest is incomplete.";
        if (manifest.Resources.Count == 0 || manifest.Resources.Count > archiveOptions.MaxResources + 1 ||
            manifest.Resources.Any(resource => resource is null))
            return "The snapshot manifest contains an invalid resource count.";
        if (manifest.Resources.Count(resource => resource.Path == "index.html" &&
                resource.Role == SnapshotResourceRoles.RootDocument) != 1)
            return "The snapshot manifest does not define exactly one root document.";
        var root = manifest.Resources.Single(resource => resource.Path == "index.html" &&
            resource.Role == SnapshotResourceRoles.RootDocument);
        if (!string.Equals(root.Sha256, manifest.Provenance.IndexSha256, StringComparison.OrdinalIgnoreCase))
            return "The root-document hash does not match the snapshot provenance.";
        if (manifest.Resources.Select(resource => resource.Path).Distinct(StringComparer.Ordinal).Count() !=
            manifest.Resources.Count)
            return "The snapshot manifest contains duplicate resource paths.";

        foreach (var resource in manifest.Resources)
        {
            if (!IsSafeRelativePath(resource.Path) ||
                !IsDisplayResource(resource.Path) ||
                !string.Equals(resource.ObjectKey, $"{contentPrefix}/{resource.Path}", StringComparison.Ordinal) ||
                resource.Role is not (SnapshotResourceRoles.RootDocument or
                    SnapshotResourceRoles.NestedDocument or SnapshotResourceRoles.Asset))
                return $"The snapshot manifest contains an unsafe resource: {resource.Path}";
            if (resource.Role is SnapshotResourceRoles.RootDocument or SnapshotResourceRoles.NestedDocument &&
                !IsHtml(resource.ContentType))
                return $"The snapshot manifest assigns a document role to a non-HTML resource: {resource.Path}";
            var stored = await objectStore.GetAsync(resource.ObjectKey, cancellationToken);
            if (stored is null) return $"The snapshot resource is missing: {resource.Path}";
            await using (stored.Content)
            {
                var hash = await HashAsync(stored.Content, cancellationToken);
                if (!string.Equals(hash, resource.Sha256, StringComparison.OrdinalIgnoreCase))
                    return $"The snapshot resource hash is invalid: {resource.Path}";
            }
        }

        if (!TryRelativePath(contentPrefix, manifest.Provenance.SourceObjectKey, out var sourcePath) ||
            !IsSource(sourcePath))
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

    private async Task<SnapshotMigrationResult> MigrateAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        var assessment = await AssessAsync(snapshotId, cancellationToken);
        if (assessment.State == SnapshotMigrationState.Current)
            return new(snapshotId, "migrate", true, "Already current; no changes were made.");
        if (assessment.State != SnapshotMigrationState.Migratable)
            return new(snapshotId, "migrate", false, assessment.Message);

        var snapshot = await database.Snapshots.AsNoTracking().SingleAsync(item => item.Id == snapshotId,
            cancellationToken);
        if (!string.Equals(snapshot.ContentPrefix, assessment.ContentPrefix, StringComparison.Ordinal))
            return new(snapshotId, "migrate", false, "The snapshot changed after migration assessment.");
        var originalPrefix = snapshot.ContentPrefix!;
        var originalObjects = await ListGenerationAsync(originalPrefix, cancellationToken);
        var legacy = await ReadLegacyMetadataAsync(originalPrefix, cancellationToken);
        var resourceCandidates = originalObjects.Where(item =>
            TryRelativePath(originalPrefix, item.Key, out var relativePath) && IsDisplayResource(relativePath))
            .ToArray();
        var resourceDescriptors = new Dictionary<string, SnapshotMigrationResourceDescriptor>(StringComparer.Ordinal);
        foreach (var item in resourceCandidates)
        {
            TryRelativePath(originalPrefix, item.Key, out var relativePath);
            legacy.Resources.TryGetValue(item.Key, out var previous);
            resourceDescriptors[item.Key] = new(
                relativePath,
                relativePath == "index.html" ? legacy.OriginalUrl ?? snapshot.OriginUrl : previous?.SourceUrl,
                previous?.ContentType ?? string.Empty);
        }
        var sourceMap = SnapshotMigrationNormalizer.BuildSourceMap(resourceDescriptors.Values);
        var migrationId = Guid.NewGuid();
        var migratedPrefix = $"snapshots/{snapshotId:N}/generations/manifest-v4/{migrationId:N}";
        var migratedKeys = new List<string>();
        long migratedStorageBytes = 0;
        var journalPrepared = false;
        var activated = false;

        try
        {
            foreach (var item in originalObjects)
            {
                if (!TryRelativePath(originalPrefix, item.Key, out var relativePath) ||
                    relativePath == "manifest.json" || IsDisplayResource(relativePath))
                    continue;
                var destination = $"{migratedPrefix}/{relativePath}";
                migratedStorageBytes = AddStorageBytes(migratedStorageBytes, item.Length);
                await objectStore.CopyAsync(item.Key, destination, cancellationToken);
                migratedKeys.Add(destination);
            }

            var resources = new List<SnapshotResource>(resourceCandidates.Length);
            string? indexHash = null;
            byte[]? originalIndexBytes = null;
            string? originalIndexContentType = null;
            var containsExecutableContent = false;
            foreach (var item in resourceCandidates)
            {
                var stored = await objectStore.GetAsync(item.Key, cancellationToken)
                    ?? throw new InvalidDataException($"Snapshot object disappeared during migration: {item.Key}");
                byte[] bytes;
                await using (stored.Content)
                    bytes = await ReadBytesAsync(stored.Content, archiveOptions.MaxStorageBytes, cancellationToken);

                var descriptor = resourceDescriptors[item.Key];
                var sourceContentType = string.IsNullOrWhiteSpace(descriptor.ContentType)
                    ? stored.ContentType : descriptor.ContentType;
                if (string.IsNullOrWhiteSpace(sourceContentType))
                    sourceContentType = "application/octet-stream";
                descriptor = descriptor with { ContentType = sourceContentType };
                if (descriptor.Path == "index.html")
                {
                    originalIndexBytes = bytes;
                    originalIndexContentType = sourceContentType;
                }

                var normalized = SnapshotMigrationNormalizer.Normalize(descriptor, bytes, sourceMap);
                var hash = Hash(normalized.Body);
                if (descriptor.Path == "index.html")
                    indexHash = hash;
                containsExecutableContent |= normalized.ContainsExecutableContent;

                var destination = $"{migratedPrefix}/{descriptor.Path}";
                migratedStorageBytes = AddStorageBytes(migratedStorageBytes, normalized.Body.LongLength);
                await PutAsync(destination, normalized.Body, normalized.ContentType, cancellationToken);
                migratedKeys.Add(destination);
                resources.Add(new()
                {
                    Path = descriptor.Path,
                    SourceUrl = descriptor.SourceUrl,
                    ContentType = normalized.ContentType,
                    ObjectKey = destination,
                    Sha256 = hash,
                    Role = normalized.Role
                });
            }
            if (indexHash is null || originalIndexBytes is null || originalIndexContentType is null)
                throw new InvalidDataException("index.html could not be read.");

            var sourceItem = originalObjects.FirstOrDefault(item =>
                TryRelativePath(originalPrefix, item.Key, out var relativePath) && IsSource(relativePath));
            string sourceKey;
            string sourceHash;
            string sourceType;
            if (sourceItem is null)
            {
                sourceKey = $"{migratedPrefix}/source.legacy.html";
                migratedStorageBytes = AddStorageBytes(migratedStorageBytes, originalIndexBytes.LongLength);
                await PutAsync(sourceKey, originalIndexBytes, originalIndexContentType, cancellationToken);
                migratedKeys.Add(sourceKey);
                sourceHash = Hash(originalIndexBytes);
                sourceType = originalIndexContentType;
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

            var manifest = new SnapshotManifest
            {
                Version = SnapshotManifest.CurrentVersion,
                Provenance = new()
                {
                    DerivedDisplayDocument = true,
                    SourceObjectKey = sourceKey,
                    SourceSha256 = sourceHash,
                    IndexSha256 = indexHash,
                    SourceUrl = legacy.OriginalUrl ?? snapshot.OriginUrl,
                    SourceMimeType = sourceType,
                    CapturedAt = snapshot.CaptureCompletedAt ?? snapshot.CreatedAt,
                    DocumentState = "legacy-migrated",
                    RedirectChain = []
                },
                Interaction = new()
                {
                    Normalizer = SnapshotDocumentNormalizer.Version,
                    ContainsExecutableContent = containsExecutableContent,
                    Diagnostic = null
                },
                Resources = resources,
                MissingUrls = legacy.MissingUrls,
                MissingUrlCount = legacy.MissingUrlCount,
                Incomplete = legacy.Incomplete
            };
            var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJson);
            var manifestKey = $"{migratedPrefix}/manifest.json";
            migratedStorageBytes = AddStorageBytes(migratedStorageBytes, manifestBytes.LongLength);
            await PutAsync(manifestKey, manifestBytes, "application/json", cancellationToken);
            migratedKeys.Add(manifestKey);

            var validationError = await ValidateCurrentManifestAsync(migratedPrefix, manifest, cancellationToken);
            if (validationError is not null)
                throw new InvalidDataException($"The prepared snapshot generation is invalid: {validationError}");

            var journal = new SnapshotMigrationJournal
            {
                SnapshotId = snapshotId,
                MigrationId = migrationId,
                Status = SnapshotMigrationJournal.Prepared,
                OriginalContentPrefix = originalPrefix,
                MigratedContentPrefix = migratedPrefix,
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
                    item.ContentPrefix == originalPrefix)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.ContentPrefix, migratedPrefix)
                    .SetProperty(item => item.StorageBytes, migratedStorageBytes)
                    .SetProperty(item => item.ResourceCount, Math.Max(0, resources.Count - 1)), cancellationToken);
            if (updated != 1)
                return new(snapshotId, "migrate", false,
                    "The snapshot changed before activation; the prepared generation can be removed with clean.");
            activated = true;

            await WriteJournalAsync(journal with
            {
                Status = SnapshotMigrationJournal.Applied,
                AppliedAt = DateTimeOffset.UtcNow
            }, cancellationToken);
            return new(snapshotId, "migrate", true, "Activated a validated manifest-backed snapshot generation.");
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
            return new(snapshotId, "migrate", false,
                activated
                    ? $"The new generation is active, but journal finalization failed: {exception.Message}"
                    : $"Migration stopped without changing the active generation: {exception.Message}");
        }
    }

    private async Task<SnapshotMigrationResult> RestoreAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        SnapshotMigrationJournal? journal;
        try { journal = await ReadJournalAsync(snapshotId, cancellationToken); }
        catch (InvalidDataException exception) { return new(snapshotId, "restore", false, exception.Message); }
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

    private async Task<SnapshotMigrationResult> CleanAsync(Guid snapshotId, CancellationToken cancellationToken)
    {
        SnapshotMigrationJournal? journal;
        try { journal = await ReadJournalAsync(snapshotId, cancellationToken); }
        catch (InvalidDataException exception) { return new(snapshotId, "clean", false, exception.Message); }
        if (journal is null) return new(snapshotId, "clean", true, "No migration data exists.");
        var prefix = await database.Snapshots.AsNoTracking().Where(item => item.Id == snapshotId)
            .Select(item => item.ContentPrefix).SingleOrDefaultAsync(cancellationToken);
        IReadOnlyList<string> inactiveKeys;
        if (prefix == journal.MigratedContentPrefix)
        {
            var assessment = await AssessAsync(snapshotId, cancellationToken);
            if (assessment.State != SnapshotMigrationState.Current)
                return new(snapshotId, "clean", false,
                    "The migrated generation is not valid; clean would remove the restore source.");
            inactiveKeys = journal.OriginalObjectKeys;
        }
        else if (prefix == journal.OriginalContentPrefix)
            inactiveKeys = journal.MigratedObjectKeys;
        else
            return new(snapshotId, "clean", false, "The active generation does not match the migration journal.");

        foreach (var key in inactiveKeys.OrderByDescending(key => key.Length))
            await objectStore.DeleteAsync(key, cancellationToken);
        await objectStore.DeleteAsync(JournalKey(snapshotId), cancellationToken);
        return new(snapshotId, "clean", true,
            prefix == journal.MigratedContentPrefix
                ? "Removed the original generation and migration journal; restore is no longer possible."
                : "Removed the inactive migrated generation and migration journal.");
    }

    private async Task<SnapshotMigrationJournal?> ReadJournalAsync(Guid snapshotId,
        CancellationToken cancellationToken)
    {
        var stored = await objectStore.GetAsync(JournalKey(snapshotId), cancellationToken);
        if (stored is null) return null;
        SnapshotMigrationJournal? journal;
        try
        {
            await using (stored.Content)
                journal = await JsonSerializer.DeserializeAsync(stored.Content,
                    SnapshotMigrationJsonContext.Default.SnapshotMigrationJournal, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The migration journal is not valid JSON.", exception);
        }
        if (journal is null) throw new InvalidDataException("The migration journal is empty.");
        SnapshotMigrationJournalValidator.Validate(journal, snapshotId);
        return journal;
    }

    private Task WriteJournalAsync(SnapshotMigrationJournal journal, CancellationToken cancellationToken) =>
        PutAsync(JournalKey(journal.SnapshotId),
            JsonSerializer.SerializeToUtf8Bytes(journal, SnapshotMigrationJsonContext.Default.SnapshotMigrationJournal),
            "application/json", cancellationToken);

    private async Task<LegacySnapshotMetadata> ReadLegacyMetadataAsync(string prefix,
        CancellationToken cancellationToken)
    {
        var stored = await objectStore.GetAsync($"{prefix}/manifest.json", cancellationToken);
        if (stored is null)
            return new(new Dictionary<string, LegacySnapshotResource>(), [], 0, false, null);
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
                            resource.TryGetProperty("sourceUrl", out var sourceUrl)
                                ? sourceUrl.GetString()
                                : resource.TryGetProperty("url", out var url) ? url.GetString() : null,
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
                var originalUrl = root.TryGetProperty("originalUrl", out var originalUrlElement)
                    ? originalUrlElement.GetString()
                    : root.TryGetProperty("provenance", out var provenance) &&
                      provenance.ValueKind == JsonValueKind.Object &&
                      provenance.TryGetProperty("sourceUrl", out var provenanceUrl)
                        ? provenanceUrl.GetString()
                        : null;
                return new(resources, missingUrls, missingCount, incomplete, originalUrl);
            }
        }
        catch (JsonException)
        {
            return new(new Dictionary<string, LegacySnapshotResource>(), [], 0, true, null);
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

    private static bool IsSnapshotContentPrefix(Guid snapshotId, string prefix)
    {
        var root = $"snapshots/{snapshotId:N}";
        return IsSafeRelativePath(prefix) &&
            (string.Equals(prefix, root, StringComparison.Ordinal) ||
             prefix.StartsWith(root + "/", StringComparison.Ordinal));
    }

    private static bool IsDisplayResource(string path) =>
        path != "manifest.json" && path != "screenshot.png" && !IsSource(path);

    private static bool IsHtml(string contentType) =>
        contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
        contentType.StartsWith("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);

    private static bool IsSource(string path) =>
        !path.Contains('/') && path.StartsWith("source.", StringComparison.OrdinalIgnoreCase);

    private static string JournalKey(Guid snapshotId) => $"{JournalPrefix}/{snapshotId:N}.json";

    private async Task PutAsync(string key, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream(bytes, writable: false);
        await objectStore.PutAsync(key, stream, contentType, cancellationToken);
    }

    private long AddStorageBytes(long currentBytes, long additionalBytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(additionalBytes);
        if (additionalBytes > archiveOptions.MaxStorageBytes - currentBytes)
            throw new InvalidDataException("The normalized snapshot generation exceeds the storage limit.");
        return currentBytes + additionalBytes;
    }

    private bool FitsStorageLimit(IEnumerable<ArchiveObjectInfo> objects)
    {
        long total = 0;
        foreach (var item in objects)
        {
            if (item.Length < 0 || item.Length > archiveOptions.MaxStorageBytes - total)
                return false;
            total += item.Length;
        }
        return true;
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
