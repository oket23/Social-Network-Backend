using SocialNetwork.Application.Common.Exceptions;
using SocialNetwork.Domain.Posts;

namespace SocialNetwork.Application.Posts;

/// <summary>Перевірка файлів поста: фото або відео, з окремими лімітами розміру для кожного типу.</summary>
public static class PostMediaFiles
{
    public const long MaxImageSizeBytes = 5 * 1024 * 1024;
    public const long MaxVideoSizeBytes = 50 * 1024 * 1024;

    private static readonly Dictionary<string, string> ImageExtensions = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private static readonly Dictionary<string, string> VideoExtensions = new()
    {
        ["video/mp4"] = ".mp4",
        ["video/quicktime"] = ".mov",
        ["video/webm"] = ".webm",
    };

    /// <summary>Перевіряє тип і розмір файлу; повертає тип медіа й розширення для імені файлу в сховищі.</summary>
    public static (MediaType Type, string Extension) GetMediaOrThrow(string contentType, long length)
    {
        if (length == 0)
        {
            throw new BadRequestException("Файл порожній.");
        }

        if (ImageExtensions.TryGetValue(contentType, out var imageExtension))
        {
            if (length > MaxImageSizeBytes)
            {
                throw new BadRequestException("Фото завелике (максимум 5 МБ).");
            }

            return (MediaType.Image, imageExtension);
        }

        if (VideoExtensions.TryGetValue(contentType, out var videoExtension))
        {
            if (length > MaxVideoSizeBytes)
            {
                throw new BadRequestException("Відео завелике (максимум 50 МБ).");
            }

            return (MediaType.Video, videoExtension);
        }

        throw new BadRequestException("Дозволені формати: JPEG, PNG, WEBP (фото), MP4, MOV, WEBM (відео).");
    }
}
