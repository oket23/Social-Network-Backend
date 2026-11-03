using SocialNetwork.Application.Common.Responses;
using SocialNetwork.Application.Posts.Requests;
using SocialNetwork.Application.Posts.Responses;

namespace SocialNetwork.Application.Posts;

public interface IPostService
{
    Task<PostResponse> Create(CreatePostRequest request, CancellationToken ct = default);

    Task<PostResponse> GetById(Guid id, CancellationToken ct = default);

    /// <summary>Змінює підпис. Лише автор, інакше 403.</summary>
    Task<PostResponse> Update(Guid id, UpdatePostRequest request, CancellationToken ct = default);

    /// <summary>Видаляє пост і його фото зі сховища. Лише автор, інакше 403.</summary>
    Task Delete(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Стрічка: поки немає підписок — пости всіх публічних акаунтів і власні, новіші зверху.
    /// Після модуля Follow — пости тих, на кого підписаний, і власні.
    /// </summary>
    Task<CursorPage<PostResponse>> GetFeed(GetPostsRequest request, CancellationToken ct = default);

    /// <summary>Пости користувача (сітка в профілі). Приватний акаунт бачить лише власник.</summary>
    Task<CursorPage<PostResponse>> GetUserPosts(string username, GetPostsRequest request, CancellationToken ct = default);
}
