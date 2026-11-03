using SocialNetwork.Domain.Common;

namespace SocialNetwork.Domain.Posts;

/// <summary>Одне фото поста. Сам файл лежить у S3-сховищі, тут — лише його публічний URL.</summary>
public class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }

    public Post Post { get; set; } = null!;

    public required string Url { get; set; }

    /// <summary>Порядок у каруселі: 0, 1, 2...</summary>
    public int Position { get; set; }
}
