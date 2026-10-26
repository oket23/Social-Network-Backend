using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SocialNetwork.Application.Common.Abstractions;

namespace SocialNetwork.Infrastructure.Storage;

/// <summary>
/// Зберігає файли в S3-сумісному сховищі (S3 — стандартний протокол, який підтримують AWS, Supabase, MinIO, Cloudflare R2).
/// Bucket має бути публічним на читання: фронт показує файли напряму за URL.
/// </summary>
public class S3FileStorage : IFileStorage, IDisposable
{
    private readonly StorageSettings _settings;
    private readonly IAmazonS3 _client;

    public S3FileStorage(IOptions<StorageSettings> settings)
    {
        _settings = settings.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _settings.ServiceUrl,
            AuthenticationRegion = _settings.Region,
            // Адреса виду host/bucket/key замість bucket.host/key — так працюють MinIO і Supabase.
            ForcePathStyle = true,
            // Нові версії AWS SDK за замовчуванням додають контрольні суми, які не всі S3-сумісні сервіси розуміють.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        _client = new AmazonS3Client(new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey), config);
    }

    public async Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = path,
            InputStream = content,
            ContentType = contentType,
            // Не всі S3-сумісні сервіси підтримують chunked-завантаження з підписом кожного шматка.
            UseChunkEncoding = false,
        }, ct);

        return $"{PublicBaseUrl}/{path}";
    }

    public async Task DeleteAsync(string url, CancellationToken ct = default)
    {
        // Видаляємо лише файли з нашого bucket-а (не чіпаємо зовнішні URL).
        if (!url.StartsWith(PublicBaseUrl + "/", StringComparison.Ordinal))
        {
            return;
        }

        var key = url[(PublicBaseUrl.Length + 1)..];
        await _client.DeleteObjectAsync(_settings.BucketName, key, ct);
    }

    public void Dispose() => _client.Dispose();

    private string PublicBaseUrl => _settings.PublicBaseUrl.TrimEnd('/');
}
