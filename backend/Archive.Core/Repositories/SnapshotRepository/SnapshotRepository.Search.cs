using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Mapping;
using Archive.Core.Models;
using Archive.Core.Models.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.SnapshotRepository;

internal sealed partial class SnapshotRepository
{
    public async Task<SnapshotSearchResult> SearchAsync(SnapshotSearch search, CancellationToken cancellationToken = default)
    {
        var errors = ModelValidation.GetErrors(search);
        if (errors.Count > 0)
            return new SnapshotSearchResult.Invalid(errors);

        string? normalizedSourceUrl = null;
        if (!string.IsNullOrWhiteSpace(search.SourceUrl))
            UrlNormalizer.TryNormalizeSource(search.SourceUrl, out normalizedSourceUrl, out _);

        var textTerms = (search.Text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tags = (search.Tags ?? [])
            .Select(SnapshotValidation.NormalizeTag)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var published = database.Snapshots.AsNoTracking()
            .Where(snapshot => snapshot.ModerationStatus == SnapshotModerationStatus.Approved
                && !snapshot.IsPrivate
                && snapshot.Quality != null);
        var query = published;
        foreach (var term in textTerms)
        {
            var pattern = ContainsPattern(term);
            query = query.Where(snapshot => EF.Functions.ILike(snapshot.Title, pattern, "\\") ||
                (snapshot.Description != null && EF.Functions.ILike(snapshot.Description, pattern, "\\")) ||
                EF.Functions.ILike(snapshot.OriginUrl ?? string.Empty, pattern, "\\") ||
                snapshot.SnapshotTags.Any(snapshotTag => EF.Functions.ILike(snapshotTag.Tag.Value, pattern, "\\")));
        }

        if (normalizedSourceUrl is not null)
            query = query.Where(snapshot => snapshot.OriginUrl == normalizedSourceUrl);

        if (!string.IsNullOrWhiteSpace(search.Title))
        {
            var pattern = ContainsPattern(search.Title);
            query = query.Where(snapshot => EF.Functions.ILike(snapshot.Title, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(search.Description))
        {
            var pattern = ContainsPattern(search.Description);
            query = query.Where(snapshot => snapshot.Description != null && EF.Functions.ILike(snapshot.Description, pattern, "\\"));
        }

        foreach (var tag in tags)
        {
            var pattern = ExactPattern(tag);
            query = query.Where(snapshot => snapshot.SnapshotTags.Any(snapshotTag => EF.Functions.ILike(snapshotTag.Tag.Value, pattern, "\\")));
        }

        if (search.SourceType is not null)
            query = query.Where(snapshot => snapshot.SourceType == search.SourceType);

        if (search.Quality is not null)
            query = query.Where(snapshot => snapshot.Quality == search.Quality);

        // Distribution applies metadata filters, but deliberately precedes the capture-time window.
        var years = await query
            .GroupBy(snapshot => snapshot.CreatedAt.Year)
            .OrderBy(group => group.Key)
            .Select(group => new SnapshotCaptureYear(group.Key, group.Count()))
            .ToArrayAsync(cancellationToken);

        var distribution = new SnapshotCaptureDistribution(years.Sum(year => year.Count),
            years.Length == 0 ? null : years[0].Year, years.Length == 0 ? null : years[^1].Year, years);

        var indexTotal = await published.CountAsync(cancellationToken);
        var recentCaptureTimes = await published
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .ThenByDescending(snapshot => snapshot.Id)
            .Select(snapshot => snapshot.CreatedAt)
            .Take(4)
            .ToArrayAsync(cancellationToken);

        if (search.CapturedFrom is not null)
        {
            var from = search.CapturedFrom.Value.ToUniversalTime();
            query = query.Where(snapshot => snapshot.CreatedAt >= from);
        }

        if (search.CapturedUntil is not null)
        {
            var until = search.CapturedUntil.Value.ToUniversalTime();
            query = query.Where(snapshot => snapshot.CreatedAt < until);
        }

        var total = await query.CountAsync(cancellationToken);
        var ordered = search.Order == SnapshotSortOrder.Asc
            ? query.OrderBy(snapshot => snapshot.CreatedAt).ThenBy(snapshot => snapshot.Id)
            : query.OrderByDescending(snapshot => snapshot.CreatedAt).ThenByDescending(snapshot => snapshot.Id);

        var page = await ordered
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ProjectToDetails()
            .ToArrayAsync(cancellationToken);
        return new SnapshotSearchResult.Found(new(page, total, search.Page, search.PageSize,
            indexTotal, recentCaptureTimes, distribution));
    }

    private static string ContainsPattern(string value) => $"%{ExactPattern(value.Trim())}%";
    private static string ExactPattern(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
