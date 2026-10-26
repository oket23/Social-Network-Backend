namespace SocialNetwork.Infrastructure.Storage;

/// <summary>
/// Секція "Storage" — будь-яке S3-сумісне сховище.
/// Локально — MinIO у Docker, на staging — Supabase Storage (S3 API).
/// </summary>
public class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Адреса S3 API: http://localhost:9000 (MinIO) або https://&lt;project&gt;.supabase.co/storage/v1/s3.</summary>
    public string ServiceUrl { get; set; } = "http://localhost:9000";

    public string Region { get; set; } = "us-east-1";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string BucketName { get; set; } = "media";

    /// <summary>
    /// Публічна адреса bucket-а, з якої браузер завантажує файли:
    /// http://localhost:9000/media (MinIO) або https://&lt;project&gt;.supabase.co/storage/v1/object/public/media.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:9000/media";
}
