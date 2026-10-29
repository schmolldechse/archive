using Amazon.S3;
using Amazon.S3.Model;
using Archive.Core.Options;

namespace Archive.Core.Storage;

internal sealed class S3ArchiveObjectStore(IAmazonS3 client, ObjectStorageOptions options) : IArchiveObjectStore
{
    private readonly string _bucket = options.Bucket;

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default) =>
        client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);

    public async Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetObjectAsync(_bucket, key, cancellationToken);
            return new ArchiveObject(response.ResponseStream,
                response.Headers.ContentType ?? "application/octet-stream", response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ArchiveObjectMetadata?> GetMetadataAsync(string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.GetObjectMetadataAsync(_bucket, key, cancellationToken);
            return new ArchiveObjectMetadata(response.Headers.ContentType ?? "application/octet-stream",
                response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<ArchiveObject?> GetRangeAsync(string key, long offset, long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        try
        {
            var response = await client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                ByteRange = new ByteRange(offset, checked(offset + length - 1))
            }, cancellationToken);
            return new ArchiveObject(response.ResponseStream,
                response.Headers.ContentType ?? "application/octet-stream", response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<ArchiveObjectInfo>> ListAsync(string prefix,
        CancellationToken cancellationToken = default)
    {
        var objects = new List<ArchiveObjectInfo>();
        string? continuationToken = null;
        do
        {
            var response = await client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken
            }, cancellationToken);
            objects.AddRange((response.S3Objects ?? []).Select(item => new ArchiveObjectInfo(item.Key, item.Size ?? 0)));
            continuationToken = response.NextContinuationToken;
        } while (!string.IsNullOrEmpty(continuationToken));

        return objects;
    }

    public Task CopyAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default) =>
        client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _bucket,
            SourceKey = sourceKey,
            DestinationBucket = _bucket,
            DestinationKey = destinationKey,
            MetadataDirective = S3MetadataDirective.COPY
        }, cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(_bucket, key, cancellationToken);
}
