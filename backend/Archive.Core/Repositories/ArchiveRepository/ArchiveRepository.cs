using Archive.Core.Entities;
using Archive.Core.Jobs;
using Archive.Core.Models;
using Archive.Core.Models.Archive;
using Archive.Core.Options;
using Archive.Core.Storage;
using Archive.Core.Uploads;
using Microsoft.EntityFrameworkCore;

namespace Archive.Core.Repositories.ArchiveRepository;

internal sealed partial class ArchiveRepository(
    DataContext database,
    IArchiveObjectStore objectStore,
    UploadFormatSelector uploadFormatSelector,
    ArchiveOptions options) : IArchiveRepository
{
    public Task<ArchiveCreationResult> CreateFromUrlAsync(ArchiveCreationRequest request, CancellationToken cancellationToken = default) =>
        CreateAsync(request, null, cancellationToken);

    public Task<ArchiveCreationResult> CreateFromFileAsync(ArchiveCreationRequest request, ArchiveUpload upload, CancellationToken cancellationToken = default) =>
        CreateAsync(request, upload, cancellationToken);

    private async Task<ArchiveCreationResult> CreateAsync(ArchiveCreationRequest request, ArchiveUpload? upload, CancellationToken cancellationToken)
    {
        var errors = new List<FieldValidationError>();

        if (!UrlNormalizer.TryNormalizeSource(request.OriginUrl, out var originUrl, out var urlError))
            errors.Add(new("originUrl", urlError));

        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is 0 or > 500)
            errors.Add(new("title", "A title of at most 500 characters is required."));
        if (title.Any(char.IsControl))
            errors.Add(new("title", "The title cannot contain control characters."));

        if (request.Description?.Length > 10_000)
            errors.Add(new("description", "The description must contain at most 10000 characters."));

        var tags = (request.Tags ?? [])
            .Select(value => (value ?? string.Empty)
            .Trim()
            .TrimStart('#')
            .ToLowerInvariant())
            .ToArray();
        if (tags.Length > 30)
            errors.Add(new("tags", "At most 30 tags are allowed."));
        if (tags.Any(tag => tag.Length is 0 or > 100))
            errors.Add(new("tags", "Each tag must contain between 1 and 100 characters."));
        if (tags.Any(tag => tag.Any(char.IsControl)))
            errors.Add(new("tags", "Tags cannot contain control characters."));

        if (errors.Count > 0)
            return new ArchiveCreationResult.Invalid(errors);

        IUploadFormatProbe? format = null;
        if (upload is not null)
        {
            if (upload.Length > options.MaxUploadBytes)
                return new ArchiveCreationResult.UploadTooLarge();
            if (upload.Length <= 0)
                return new ArchiveCreationResult.Invalid([new("file", "Provide a non-empty .html, .mhtml or .webarchive file.")]);
            var prefix = new byte[(int)Math.Min(upload.Length, 8_192)];
            await using (var stream = upload.OpenReadStream())
            {
                var read = 0;
                while (read < prefix.Length)
                {
                    var count = await stream.ReadAsync(prefix.AsMemory(read), cancellationToken);
                    if (count == 0) break;
                    read += count;
                }
                format = uploadFormatSelector.Select(upload.FileName, prefix.AsSpan(0, read));
            }

            if (format is null)
                return new ArchiveCreationResult.Invalid([new("file", "The file format is unsupported or does not match its extension.")]);
        }

        var description = request.Description?.Trim();
        var job = new ArchiveJob
        {
            SourceType = format?.SourceType ?? SourceType.Url,
            OriginUrl = originUrl,
            Title = title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            Status = JobStatus.Queued,
            ProgressStep = ProgressStep.SourceCheck,
            ProgressPercent = 0
        };
        job.Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).Select(value => new ArchiveJobTag { JobId = job.Id, Value = value }).ToList();

        var projectKey = UrlNormalizer.ProjectKey(originUrl);
        if (!await database.Projects.AnyAsync(project => project.ProjectKey == projectKey, cancellationToken))
            database.Projects.Add(new Project { ProjectKey = projectKey });

        if (upload is not null)
        {
            job.UploadedObjectKey = $"jobs/{job.Id:N}/source{format!.Extension}";
            await using var stream = upload.OpenReadStream();
            await objectStore.PutAsync(job.UploadedObjectKey, stream, format.ContentType, cancellationToken);
        }

        database.ArchiveJobs.Add(job);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (job.UploadedObjectKey is not null)
            {
                try
                {
                    if (!await database.ArchiveJobs.AsNoTracking().AnyAsync(archive => archive.Id == job.Id, CancellationToken.None))
                        await objectStore.DeleteAsync(job.UploadedObjectKey, CancellationToken.None);
                }
                catch { /* An ambiguous database result must preserve the upload for a possible queued job. */ }
            }
            throw;
        }

        return new ArchiveCreationResult.Created(await BuildStatusAsync(job, cancellationToken));
    }
}
