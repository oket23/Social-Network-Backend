using SocialNetwork.Application.Common.Exceptions;

namespace SocialNetwork.Application.Common.Files;

/// <summary>Спільні правила для зображень (аватари, фото постів).</summary>
public static class ImageFiles
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    /// <summary>Перевіряє розмір і тип файлу; повертає розширення для імені файлу в сховищі.</summary>
    public static string GetExtensionOrThrow(string contentType, long length)
    {
        if (length == 0)
        {
            throw new BadRequestException("Файл порожній.");
        }

        if (length > MaxSizeBytes)
        {
            throw new BadRequestException("Файл завеликий (максимум 5 МБ).");
        }

        if (!Extensions.TryGetValue(contentType, out var extension))
        {
            throw new BadRequestException("Дозволені формати: JPEG, PNG, WEBP.");
        }

        return extension;
    }
}
