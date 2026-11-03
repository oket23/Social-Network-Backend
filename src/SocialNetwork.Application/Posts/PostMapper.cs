using SocialNetwork.Application.Posts.Responses;
using SocialNetwork.Domain.Posts;

namespace SocialNetwork.Application.Posts;

public static class PostMapper
{
    /// <summary>Пост має бути завантажений разом з Author і Media.</summary>
    public static PostResponse ToResponse(this Post post, Guid viewerId) => new()
    {
        Id = post.Id,
        Caption = post.Caption,
        Media = post.Media
            .OrderBy(m => m.Position)
            .Select(m => new PostMediaResponse { Url = m.Url })
            .ToList(),
        Author = new PostAuthorResponse
        {
            Id = post.Author.Id,
            Username = post.Author.UserName!,
            DisplayName = post.Author.DisplayName,
            AvatarUrl = post.Author.AvatarUrl,
        },
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt,
        IsMine = post.AuthorId == viewerId,
    };
}
