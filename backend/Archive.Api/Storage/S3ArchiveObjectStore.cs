using Amazon.S3;
using Amazon.S3.Model;
using Archive.Core.Storage;

namespace Archive.Api.Storage;

public sealed class S3ArchiveObjectStore(IConfiguration configuration) : IArchiveObjectStore
{
    private readonly string _bucket = configuration["ObjectStorage:Bucket"] ?? "snapshots";
    private readonly IAmazonS3 _client = CreateClient(configuration);

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

    private static AmazonS3Client CreateClient(IConfiguration configuration)
    {
        var accessKey = configuration["ObjectStorage:AccessKey"]?.Trim();
        var secretKey = configuration["ObjectStorage:SecretKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(accessKey) != string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException(
                "ObjectStorage:AccessKey and ObjectStorage:SecretKey must either both be configured or both be omitted.");

        var config = new AmazonS3Config
        {
            ServiceURL = configuration["ObjectStorage:ServiceUrl"] ?? "http://localhost:9000",
            ForcePathStyle = true
        };

        return string.IsNullOrWhiteSpace(accessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(accessKey, secretKey, config);
    }
}
