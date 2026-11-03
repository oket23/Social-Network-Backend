using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Posts;

namespace SocialNetwork.Application.Posts;

public interface IPostRepository : IRepository<Post>
{
    /// <summary>Для змін (з трекінгом), разом з фото.</summary>
    Task<Post?> GetWithMedia(Guid id, CancellationToken ct = default);

    /// <summary>Для показу (без трекінгу): з автором і фото.</summary>
    Task<Post?> GetDetails(Guid id, CancellationToken ct = default);

    /// <summary>Стрічка: пости публічних акаунтів і власні, новіші за <paramref name="before"/> не беруться.</summary>
    Task<List<Post>> GetFeed(Guid viewerId, DateTime? before, int take, CancellationToken ct = default);

    Task<List<Post>> GetByAuthor(Guid authorId, DateTime? before, int take, CancellationToken ct = default);
}
