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

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        client.DeleteObjectAsync(_bucket, key, cancellationToken);
}
