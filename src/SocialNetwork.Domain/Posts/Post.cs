using SocialNetwork.Domain.Common;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Domain.Posts;

/// <summary>Пост: підпис + 1–10 фото (карусель, як в Instagram).</summary>
public class Post : BaseEntity, ISoftDeletable
{
    public Guid AuthorId { get; set; }

    public User Author { get; set; } = null!;

    public string? Caption { get; set; }

    /// <summary>Коли востаннє змінювали підпис (null — не змінювали).</summary>
    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<PostMedia> Media { get; set; } = [];
}
