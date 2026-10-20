using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Archive.Core;
using Archive.Core.Entities;
using Archive.Core.Options;
using Archive.Core.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Archive.SnapshotMigration;

internal sealed record MigrationRunResult(string Summary, int Failed);

/// <summary>Moves legacy snapshot objects to a staged prefix with route-neutral HTML and CSS references.</summary>
internal sealed class SnapshotResourceMigrator(DataContext database, IArchiveObjectStore objectStore,
    ArchiveOptions archiveOptions, ILogger<SnapshotResourceMigrator> logger)
{
    private const string RelativeSuffix = "/relative-v1";
    private const string ResourceReferenceMode = "relative-v1";
    private const string ManifestName = "manifest.json";
    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { WriteIndented = true };

    /// <summary>Validates or migrates every completed snapshot in stable, bounded batches.</summary>
    public async Task<MigrationRunResult> MigrateAsync(bool apply, int batchSize, CancellationToken token)
    {
        var examined = 0;
        var migrated = 0;
        var alreadyRelative = 0;
        var failed = 0;
        var offset = 0;

        while (true)
        {
            var snapshots = await database.Snapshots.AsNoTracking()
                .Where(snapshot => snapshot.Quality != null && snapshot.ContentPrefix != null)
                .OrderBy(snapshot => snapshot.Id).Skip(offset).Take(batchSize)
                .Select(snapshot => new SnapshotRow(snapshot.Id, snapshot.ContentPrefix!, snapshot.StorageBytes,
                    snapshot.OriginUrl))
                .ToArrayAsync(token);
            if (snapshots.Length == 0) break;
            offset += snapshots.Length;

            foreach (var snapshot in snapshots)
            {
                token.ThrowIfCancellationRequested();
                examined++;
                try
                {
                    var outcome = await MigrateOneAsync(snapshot, apply, token);
                    if (outcome == MigrationOutcome.AlreadyRelative) alreadyRelative++;
                    else migrated++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    failed++;
                    logger.LogError(exception, "Snapshot {SnapshotId} could not be migrated", snapshot.Id);
                }
            }
        }

        var verb = apply ? "migrated" : "would-migrate";
        return new($"examined={examined}; {verb}={migrated}; already-relative={alreadyRelative}; failed={failed}", failed);
    }

    /// <summary>Restores one snapshot's original prefix while its retained source manifest still exists.</summary>
    public async Task<MigrationRunResult> RollbackAsync(Guid snapshotId, CancellationToken token)
    {
        var snapshot = await database.Snapshots.SingleOrDefaultAsync(item => item.Id == snapshotId, token);
        if (snapshot?.ContentPrefix is null)
            return new($"snapshot={snapshotId}; failed=1; reason=not-found", 1);
        var isActiveTarget = snapshot.ContentPrefix.EndsWith(RelativeSuffix, StringComparison.Ordinal);
        var sourcePrefix = isActiveTarget
            ? snapshot.ContentPrefix[..^RelativeSuffix.Length]
            : snapshot.ContentPrefix;
        var targetPrefix = sourcePrefix + RelativeSuffix;
        var targetManifestObject = await objectStore.GetAsync($"{targetPrefix}/{ManifestName}", token);
        if (targetManifestObject is null)
        {
            if (isActiveTarget)
                return new($"snapshot={snapshotId}; failed=1; reason=target-manifest-unavailable", 1);

            JsonObject activeSourceManifest;
            long stagedSourceStorageBytes;
            try
            {
                activeSourceManifest = await ReadManifestAsync(sourcePrefix, token);
                stagedSourceStorageBytes = await VerifyManifestObjectsAsync(sourcePrefix, activeSourceManifest, token);
            }
            catch (InvalidDataException)
            {
                return new($"snapshot={snapshotId}; failed=1; reason=source-unavailable; staging-preserved=1", 1);
            }

            if (snapshot.StorageBytes is null || stagedSourceStorageBytes != snapshot.StorageBytes)
                return new($"snapshot={snapshotId}; failed=1; reason=source-storage-size-mismatch; staging-preserved=1", 1);

            var stagedKeys = GetObjectKeys(activeSourceManifest, sourcePrefix)
                .Select(key => $"{targetPrefix}/{RelativePathOfKey(sourcePrefix, key)}");
            await DeleteObjectsManifestLastAsync(stagedKeys, token);
            return new($"snapshot={snapshotId}; already-rolled-back=1; staged-objects-deleted=1; source={sourcePrefix}", 0);
        }

        await using var targetManifestStream = targetManifestObject.Content;
        var targetManifest = await ReadManifestAsync(targetManifestStream, targetPrefix, token);
        if (GetString(targetManifest, "resourceReferenceMode") != ResourceReferenceMode)
            return new($"snapshot={snapshotId}; failed=1; reason=source-unavailable", 1);

        var originalStorageBytes = targetManifest["resourceReferenceMigration"]?["originalStorageBytes"]?.GetValue<long?>();
        if (originalStorageBytes is null)
            return new($"snapshot={snapshotId}; failed=1; reason=original-storage-size-unavailable", 1);

        JsonObject sourceManifest;
        long sourceStorageBytes;
        try
        {
            sourceManifest = await ReadManifestAsync(sourcePrefix, token);
            sourceStorageBytes = await VerifyManifestObjectsAsync(sourcePrefix, sourceManifest, token);
        }
        catch (InvalidDataException)
        {
            return new($"snapshot={snapshotId}; failed=1; reason=source-unavailable", 1);
        }

        if (sourceStorageBytes != originalStorageBytes)
            return new($"snapshot={snapshotId}; failed=1; reason=source-storage-size-mismatch", 1);

        if (isActiveTarget)
        {
            var changed = await database.Snapshots.Where(item => item.Id == snapshotId && item.ContentPrefix == targetPrefix)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(item => item.ContentPrefix, sourcePrefix)
                    .SetProperty(item => item.StorageBytes, originalStorageBytes), token);
            if (changed != 1)
                return new($"snapshot={snapshotId}; failed=1; reason=content-prefix-changed", 1);
        }

        try
        {
            await DeleteObjectsManifestLastAsync(GetObjectKeys(targetManifest, targetPrefix), token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Rolled-back objects for snapshot {SnapshotId} remain for a retry", snapshotId);
            return new($"snapshot={snapshotId}; rolled-back=1; cleanup-pending=1; source={sourcePrefix}", 1);
        }

        return new($"snapshot={snapshotId}; rolled-back=1; source={sourcePrefix}; staged-objects-deleted=1", 0);
    }

    /// <summary>Reports eligible legacy objects or removes them after their retention period.</summary>
    public async Task<MigrationRunResult> CleanupAsync(int retentionDays, bool apply, CancellationToken token)
    {
        var rows = await database.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.Quality != null && snapshot.ContentPrefix != null &&
                               EF.Functions.Like(snapshot.ContentPrefix!, "%/relative-v1"))
            .OrderBy(snapshot => snapshot.Id)
            .Select(snapshot => new SnapshotRow(snapshot.Id, snapshot.ContentPrefix!, snapshot.StorageBytes, null))
            .ToArrayAsync(token);
        var eligible = 0;
        var deleted = 0;
        var failed = 0;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        foreach (var snapshot in rows)
        {
            try
            {
                var manifest = await ReadManifestAsync(snapshot.ContentPrefix, token);
                if (!TryGetMigrationDate(manifest, out var migratedAt) || migratedAt > cutoff) continue;
                var sourcePrefix = snapshot.ContentPrefix[..^RelativeSuffix.Length];
                var activeBytes = await VerifyManifestObjectsAsync(snapshot.ContentPrefix, manifest, token);
                if (snapshot.StorageBytes is not null && activeBytes != snapshot.StorageBytes)
                    throw new InvalidDataException("Active snapshot object sizes do not match the database record.");
                var sourceManifest = await ReadManifestAsync(sourcePrefix, token);
                var keys = GetObjectKeys(sourceManifest, sourcePrefix);
                eligible++;
                if (!apply) continue;
                foreach (var key in keys.OrderBy(key => key.EndsWith($"/{ManifestName}", StringComparison.Ordinal)))
                    await objectStore.DeleteAsync(key, token);
                deleted++;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failed++;
                logger.LogError(exception, "Legacy objects for snapshot {SnapshotId} could not be cleaned up", snapshot.Id);
            }
        }

        var verb = apply ? "deleted" : "would-delete";
        return new($"examined={rows.Length}; eligible={eligible}; {verb}={deleted}; failed={failed}", failed);
    }

    private async Task<MigrationOutcome> MigrateOneAsync(SnapshotRow snapshot, bool apply, CancellationToken token)
    {
        var sourcePrefix = NormalizePrefix(snapshot.ContentPrefix);
        var sourceManifest = await ReadManifestAsync(sourcePrefix, token);
        if (GetString(sourceManifest, "resourceReferenceMode") == ResourceReferenceMode)
            return MigrationOutcome.AlreadyRelative;
        if (sourcePrefix.EndsWith(RelativeSuffix, StringComparison.Ordinal))
            throw new InvalidDataException("A versioned content prefix has no route-neutral manifest marker.");

        var destinationPrefix = sourcePrefix + RelativeSuffix;
        var sourceKeys = GetObjectKeys(sourceManifest, sourcePrefix);
        var resourceObjectPaths = GetResourceObjectPaths(sourceManifest, sourcePrefix);
        var rootOriginalUrl = snapshot.OriginUrl ?? GetString(sourceManifest, "originalUrl");
        var transformedObjects = new List<TransformedObject>();
        var normalizedContentTypes = new Dictionary<string, string>(StringComparer.Ordinal);
        long nonManifestBytes = 0;

        foreach (var sourceKey in sourceKeys.Where(key => key != $"{sourcePrefix}/{ManifestName}"))
        {
            token.ThrowIfCancellationRequested();
            var relativePath = RelativePathOfKey(sourcePrefix, sourceKey);
            var stored = await objectStore.GetAsync(sourceKey, token)
                ?? throw new InvalidDataException($"Manifest object is missing: {sourceKey}");
            await using var content = stored.Content;
            var destinationKey = $"{destinationPrefix}/{relativePath}";
            var contentType = stored.ContentType ?? "application/octet-stream";

            if (IsHtmlOrCss(contentType))
            {
                var bytes = await ReadBoundedAsync(content, stored.Length, token);
                var originalUrl = FindResourceUrlForPath(sourceManifest, sourcePrefix, relativePath) ?? rootOriginalUrl;
                var rewritten = SnapshotResourceRewriter.Rewrite(bytes, contentType, snapshot.Id, relativePath,
                    originalUrl, resourceObjectPaths);
                EnsureNoLegacyReference(rewritten, snapshot.Id, relativePath);
                nonManifestBytes = checked(nonManifestBytes + rewritten.LongLength);
                var normalizedContentType = NormalizeTextContentType(contentType);
                transformedObjects.Add(new(destinationKey, normalizedContentType, rewritten));
                normalizedContentTypes[sourceKey] = normalizedContentType;
                if (apply)
                    await PutBytesAsync(destinationKey, rewritten, normalizedContentType, token);
            }
            else
            {
                var length = stored.Length ?? throw new InvalidDataException($"Object length is unknown: {sourceKey}");
                nonManifestBytes = checked(nonManifestBytes + length);
                if (apply)
                    await objectStore.CopyAsync(sourceKey, destinationKey, token);
            }
        }

        var destinationManifest = RewriteManifest(sourceManifest, sourcePrefix, destinationPrefix,
            nonManifestBytes, normalizedContentTypes, DateTimeOffset.UtcNow, snapshot.StorageBytes);
        if (!apply) return MigrationOutcome.Migrated;

        var manifestBytes = Encoding.UTF8.GetBytes(destinationManifest.ToJsonString(ManifestJsonOptions));
        var destinationManifestKey = $"{destinationPrefix}/{ManifestName}";
        await PutBytesAsync(destinationManifestKey, manifestBytes, "application/json", token);

        foreach (var item in transformedObjects)
            await VerifyObjectAsync(item.Key, item.ContentType, item.Body.LongLength, token);
        foreach (var sourceKey in sourceKeys.Where(key => key != $"{sourcePrefix}/{ManifestName}"))
        {
            var relativePath = RelativePathOfKey(sourcePrefix, sourceKey);
            var transformed = transformedObjects.Any(item => item.Key == $"{destinationPrefix}/{relativePath}");
            if (!transformed)
            {
                var source = await objectStore.GetAsync(sourceKey, token)
                    ?? throw new InvalidDataException($"Manifest object is missing: {sourceKey}");
                var length = source.Length ?? throw new InvalidDataException($"Object length is unknown: {sourceKey}");
                await source.Content.DisposeAsync();
                await VerifyObjectAsync($"{destinationPrefix}/{relativePath}", source.ContentType, length, token);
            }
        }
        await VerifyObjectAsync(destinationManifestKey, "application/json", manifestBytes.LongLength, token);

        var totalStorageBytes = checked(nonManifestBytes + manifestBytes.LongLength);
        var changed = await database.Snapshots.Where(item => item.Id == snapshot.Id && item.ContentPrefix == sourcePrefix)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.ContentPrefix, destinationPrefix)
                .SetProperty(item => item.StorageBytes, (long?)totalStorageBytes), token);
        if (changed == 0)
        {
            var activePrefix = await database.Snapshots.AsNoTracking().Where(item => item.Id == snapshot.Id)
                .Select(item => item.ContentPrefix).SingleOrDefaultAsync(token);
            if (activePrefix != destinationPrefix)
                throw new InvalidOperationException("Snapshot content prefix changed concurrently; staged objects were left inactive.");
        }
        return MigrationOutcome.Migrated;
    }

    private async Task<byte[]> ReadBoundedAsync(Stream content, long? length, CancellationToken token)
    {
        if (length is > 0 && length > archiveOptions.MaxStorageBytes)
            throw new InvalidDataException("A text resource exceeds the configured snapshot size limit.");
        using var buffer = new MemoryStream(length is > 0 and <= int.MaxValue ? (int)length.Value : 0);
        var chunk = new byte[81920];
        while (true)
        {
            var read = await content.ReadAsync(chunk, token);
            if (read == 0) break;
            if (buffer.Length + read > archiveOptions.MaxStorageBytes)
                throw new InvalidDataException("A text resource exceeds the configured snapshot size limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), token);
        }
        return buffer.ToArray();
    }

    private async Task VerifyObjectAsync(string key, string expectedContentType, long expectedLength, CancellationToken token)
    {
        var stored = await objectStore.GetAsync(key, token)
            ?? throw new InvalidDataException($"Staged object is missing: {key}");
        await using var content = stored.Content;
        if (stored.Length != expectedLength || !string.Equals(stored.ContentType, expectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Staged object metadata does not match: {key}");
    }

    /// <summary>Verifies every object referenced by the active manifest before its backup can be removed.</summary>
    private async Task<long> VerifyManifestObjectsAsync(string prefix, JsonObject manifest, CancellationToken token)
    {
        var expectedTypes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GetRequiredString(manifest, "htmlObjectKey")] = "text/html; charset=utf-8",
            [GetRequiredString(manifest, "screenshotObjectKey")] = "image/png"
        };
        if (GetString(manifest, "sourceObjectKey") is { } sourceKey)
            expectedTypes[sourceKey] = "application/octet-stream";
        if (manifest["resources"] is not JsonArray resources)
            throw new InvalidDataException("Snapshot manifest has no resources array.");
        foreach (var resource in resources)
        {
            if (resource is not JsonObject item)
                throw new InvalidDataException("Snapshot manifest contains an invalid resource entry.");
            expectedTypes[GetRequiredString(item, "objectKey")] = GetRequiredString(item, "contentType");
        }

        long totalBytes = 0;
        foreach (var key in GetObjectKeys(manifest, prefix))
        {
            var stored = await objectStore.GetAsync(key, token)
                ?? throw new InvalidDataException($"Active snapshot object is missing: {key}");
            await using var content = stored.Content;
            var relativePath = RelativePathOfKey(prefix, key);
            if (expectedTypes.TryGetValue(key, out var expectedType) &&
                !string.Equals(stored.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Active snapshot object metadata does not match: {relativePath}");
            totalBytes = checked(totalBytes + (stored.Length
                ?? throw new InvalidDataException($"Active snapshot object length is unknown: {relativePath}")));
        }

        return totalBytes;
    }

    private async Task PutBytesAsync(string key, byte[] bytes, string contentType, CancellationToken token)
    {
        await using var content = new MemoryStream(bytes, writable: false);
        await objectStore.PutAsync(key, content, contentType, token);
    }

    private async Task<JsonObject> ReadManifestAsync(string prefix, CancellationToken token)
    {
        var stored = await objectStore.GetAsync($"{prefix}/{ManifestName}", token)
            ?? throw new InvalidDataException($"Snapshot manifest is missing under {prefix}.");
        await using var content = stored.Content;
        return await ReadManifestAsync(content, prefix, token);
    }

    private async Task DeleteObjectsManifestLastAsync(IEnumerable<string> keys, CancellationToken token)
    {
        foreach (var key in keys.OrderBy(key => key.EndsWith($"/{ManifestName}", StringComparison.Ordinal)))
            await objectStore.DeleteAsync(key, token);
    }

    private async Task<JsonObject> ReadManifestAsync(Stream content, string prefix, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, token);
        return JsonNode.Parse(buffer.ToArray()) as JsonObject
            ?? throw new InvalidDataException($"Snapshot manifest is not a JSON object under {prefix}.");
    }

    private static List<string> GetObjectKeys(JsonObject manifest, string prefix)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{prefix}/{ManifestName}",
            GetRequiredString(manifest, "htmlObjectKey"),
            GetRequiredString(manifest, "screenshotObjectKey")
        };
        if (GetString(manifest, "sourceObjectKey") is { } sourceKey) keys.Add(sourceKey);
        if (manifest["resources"] is not JsonArray resources)
            throw new InvalidDataException("Snapshot manifest has no resources array.");
        foreach (var resource in resources)
        {
            if (resource is not JsonObject item || GetString(item, "objectKey") is not { } key)
                throw new InvalidDataException("Snapshot manifest contains an invalid resource entry.");
            keys.Add(key);
        }
        foreach (var key in keys) _ = RelativePathOfKey(prefix, key);
        return keys.ToList();
    }

    private static Dictionary<string, string> GetResourceObjectPaths(JsonObject manifest, string prefix)
    {
        if (manifest["resources"] is not JsonArray resources)
            throw new InvalidDataException("Snapshot manifest has no resources array.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var resource in resources)
        {
            if (resource is not JsonObject item || GetString(item, "url") is not { } url ||
                GetString(item, "objectKey") is not { } objectKey)
                throw new InvalidDataException("Snapshot manifest contains a resource without URL or object key.");
            var path = RelativePathOfKey(prefix, objectKey);
            result.TryAdd(url, path);
        }
        return result;
    }

    private static string? FindResourceUrlForPath(JsonObject manifest, string prefix, string objectPath)
    {
        if (manifest["resources"] is not JsonArray resources) return null;
        foreach (var resource in resources)
        {
            if (resource is JsonObject item && GetString(item, "url") is { } url &&
                GetString(item, "objectKey") is { } key && RelativePathOfKey(prefix, key) == objectPath)
                return url;
        }
        return null;
    }

    private static JsonObject RewriteManifest(JsonObject source, string sourcePrefix, string destinationPrefix,
        long nonManifestBytes, IReadOnlyDictionary<string, string> normalizedContentTypes, DateTimeOffset migratedAt,
        long? originalStorageBytes)
    {
        var manifest = (JsonObject)source.DeepClone();
        RewriteKey(manifest, "htmlObjectKey", sourcePrefix, destinationPrefix);
        RewriteKey(manifest, "screenshotObjectKey", sourcePrefix, destinationPrefix);
        RewriteKey(manifest, "sourceObjectKey", sourcePrefix, destinationPrefix, optional: true);
        if (manifest["resources"] is not JsonArray resources)
            throw new InvalidDataException("Snapshot manifest has no resources array.");
        foreach (var resource in resources)
        {
            if (resource is not JsonObject item) throw new InvalidDataException("Snapshot manifest contains an invalid resource entry.");
            var sourceKey = GetRequiredString(item, "objectKey");
            if (normalizedContentTypes.TryGetValue(sourceKey, out var contentType))
                item["contentType"] = contentType;
            RewriteKey(item, "objectKey", sourcePrefix, destinationPrefix);
        }
        manifest["resourceReferenceMode"] = ResourceReferenceMode;
        manifest["resourceReferenceMigration"] = new JsonObject
        {
            ["migratedAtUtc"] = migratedAt,
            ["originalStorageBytes"] = originalStorageBytes
        };
        if (manifest.ContainsKey("storageBytes")) manifest["storageBytes"] = nonManifestBytes;
        return manifest;
    }

    private static void RewriteKey(JsonObject manifest, string property, string sourcePrefix, string destinationPrefix,
        bool optional = false)
    {
        var key = GetString(manifest, property);
        if (key is null && optional) return;
        if (key is null) throw new InvalidDataException($"Snapshot manifest is missing {property}.");
        var relative = RelativePathOfKey(sourcePrefix, key);
        manifest[property] = $"{destinationPrefix}/{relative}";
    }

    private static void EnsureNoLegacyReference(byte[] bytes, Guid id, string path)
    {
        var text = new UTF8Encoding(false, true).GetString(bytes);
        var prefixes = new[] { $"/api/snapshots/{id:N}/content/", $"/api/snapshots/{id:D}/content/" };
        foreach (var prefix in prefixes)
        {
            var index = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index < 0) continue;
            var end = index;
            while (end < text.Length && !char.IsWhiteSpace(text[end]) &&
                   text[end] is not ('"' or '\'' or '<' or '>' or ')'))
                end++;
            throw new InvalidDataException($"Legacy route reference remains in {path}: {text[index..end]}");
        }
    }

    private static bool IsHtmlOrCss(string contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
        (string.Equals(mediaType.MediaType, "text/html", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(mediaType.MediaType, "text/css", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeTextContentType(string contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType) && mediaType.MediaType is not null
            ? $"{mediaType.MediaType}; charset=utf-8"
            : throw new InvalidDataException("A text snapshot resource has an invalid Content-Type.");

    private static string NormalizePrefix(string prefix)
    {
        if (prefix.StartsWith('/') || prefix.Contains('\\'))
            throw new InvalidDataException("Snapshot content prefix is not a safe relative key prefix.");
        var normalized = SnapshotResourcePath.Normalize(prefix);
        if (!string.Equals(normalized, prefix.TrimEnd('/'), StringComparison.Ordinal))
            throw new InvalidDataException("Snapshot content prefix is not canonical.");
        return normalized;
    }

    private static string RelativePathOfKey(string prefix, string key)
    {
        if (!key.StartsWith(prefix + "/", StringComparison.Ordinal) || key.Contains('\\'))
            throw new InvalidDataException("Manifest key lies outside the snapshot content prefix.");
        var relativePath = key[(prefix.Length + 1)..];
        if (!string.Equals(SnapshotResourcePath.Normalize(relativePath), relativePath, StringComparison.Ordinal))
            throw new InvalidDataException("Manifest contains a non-canonical object path.");
        return relativePath;
    }

    private static string GetRequiredString(JsonObject node, string property) =>
        GetString(node, property) ?? throw new InvalidDataException($"Snapshot manifest is missing {property}.");

    private static string? GetString(JsonObject node, string property) =>
        node[property] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static bool TryGetMigrationDate(JsonObject manifest, out DateTimeOffset migratedAt)
    {
        var value = manifest["resourceReferenceMigration"]?["migratedAtUtc"]?.GetValue<string>();
        return DateTimeOffset.TryParse(value, out migratedAt);
    }

    private enum MigrationOutcome { Migrated, AlreadyRelative }
    private sealed record SnapshotRow(Guid Id, string ContentPrefix, long? StorageBytes, string? OriginUrl);
    private sealed record TransformedObject(string Key, string ContentType, byte[] Body);
}
