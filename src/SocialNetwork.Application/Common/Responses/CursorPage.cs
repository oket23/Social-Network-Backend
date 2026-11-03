namespace SocialNetwork.Application.Common.Responses;

/// <summary>
/// Сторінка списку з курсорною пагінацією (для нескінченної стрічки).
/// Наступна сторінка: той самий запит з ?cursor={NextCursor}. NextCursor = null — далі нічого немає.
/// На відміну від ?page=2, нові пости зверху не зсувають сторінки, тож дублів у стрічці не буде.
/// </summary>
public class CursorPage<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public DateTime? NextCursor { get; init; }
}
