namespace SocialNetwork.Application.Common.Abstractions;

/// <summary>
/// Сховище файлів (аватари, далі — фото/відео постів).
/// Реалізація: S3-сумісне сховище (локально — MinIO у Docker, на staging — Supabase Storage).
/// </summary>
public interface IFileStorage
{
    /// <returns>Публічний URL завантаженого файлу.</returns>
    Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken ct = default);

    Task DeleteAsync(string url, CancellationToken ct = default);
}
