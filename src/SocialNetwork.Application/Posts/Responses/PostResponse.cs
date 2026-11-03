namespace SocialNetwork.Application.Posts.Responses;

public class PostResponse
{
    public Guid Id { get; set; }

    public string? Caption { get; set; }

    /// <summary>Фото в порядку каруселі.</summary>
    public required IReadOnlyList<PostMediaResponse> Media { get; set; }

    public required PostAuthorResponse Author { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <summary>Пост поточного користувача — фронт показує кнопки «редагувати» / «видалити».</summary>
    public bool IsMine { get; set; }
}

public class PostMediaResponse
{
    public required string Url { get; set; }
}

public class PostAuthorResponse
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    public string? AvatarUrl { get; set; }
}
