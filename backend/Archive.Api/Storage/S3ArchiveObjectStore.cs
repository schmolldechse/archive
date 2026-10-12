using Amazon.S3;
using Amazon.S3.Model;
using Archive.Core.Storage;
using Archive.Core.Options;

namespace Archive.Api.Storage;

public sealed class S3ArchiveObjectStore(ObjectStorageOptions options) : IArchiveObjectStore
{
    private readonly string _bucket = options.Bucket;
    private readonly IAmazonS3 _client = CreateClient(options);

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);
    }

    public async Task<ArchiveObject?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);
            return new ArchiveObject(response.ResponseStream, response.Headers.ContentType ?? "application/octet-stream", response.Headers.ContentLength);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        _client.DeleteObjectAsync(_bucket, key, cancellationToken);

    private static AmazonS3Client CreateClient(ObjectStorageOptions options)
    {
        var config = new AmazonS3Config
        {
            ServiceURL = options.ServiceUrl,
            ForcePathStyle = true
        };

        return string.IsNullOrWhiteSpace(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(options.AccessKey, options.SecretKey, config);
    }
}
