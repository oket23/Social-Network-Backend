using System.Globalization;
using SocialNetwork.Application.Common.Exceptions;

namespace SocialNetwork.Application.Posts;

/// <summary>
/// Непрозорий курсор сторінки: час створення + Id останнього поста.
/// Лише CreatedAt недостатньо — пости з однаковим часом створення губилися б на межі сторінок.
/// </summary>
internal static class PostCursor
{
    public static string Encode(DateTime createdAt, Guid id) =>
        $"{createdAt.Ticks.ToString(CultureInfo.InvariantCulture)}_{id:N}";

    public static (DateTime CreatedAt, Guid Id)? Decode(string? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        var parts = cursor.Split('_', 2);
        if (parts.Length == 2
            && long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            && ticks <= DateTime.MaxValue.Ticks
            && Guid.TryParse(parts[1], out var id))
        {
            return (new DateTime(ticks, DateTimeKind.Utc), id);
        }

        throw new BadRequestException("Некоректний курсор сторінки.");
    }
}
