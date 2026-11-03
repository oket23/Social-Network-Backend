namespace SocialNetwork.Application.Posts.Requests;

/// <summary>Параметри сторінки: ?cursor=...&amp;limit=20.</summary>
public class GetPostsRequest
{
    /// <summary>NextCursor з попередньої сторінки; порожньо — перша сторінка.</summary>
    public DateTimeOffset? Cursor { get; set; }

    public int Limit { get; set; } = 20;
}
