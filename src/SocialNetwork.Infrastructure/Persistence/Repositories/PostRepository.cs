using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Posts;
using SocialNetwork.Domain.Posts;
using SocialNetwork.Infrastructure.Persistence.Repositories.Base;

namespace SocialNetwork.Infrastructure.Persistence.Repositories;

public class PostRepository : RepositoryBase<Post>, IPostRepository
{
    public PostRepository(AppDbContext context) : base(context)
    {
    }

    public Task<Post?> GetWithMedia(Guid id, CancellationToken ct = default) =>
        Set.Include(p => p.Media).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Post?> GetDetails(Guid id, CancellationToken ct = default) =>
        WithDetails(Query).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<Post>> GetFeed(Guid viewerId, DateTime? before, int take, CancellationToken ct = default) =>
        Page(Query.Where(p => !p.Author.IsPrivate || p.AuthorId == viewerId), before, take, ct);

    public Task<List<Post>> GetByAuthor(Guid authorId, DateTime? before, int take, CancellationToken ct = default) =>
        Page(Query.Where(p => p.AuthorId == authorId), before, take, ct);

    private static Task<List<Post>> Page(IQueryable<Post> posts, DateTime? before, int take, CancellationToken ct)
    {
        if (before is not null)
        {
            posts = posts.Where(p => p.CreatedAt < before);
        }

        return WithDetails(posts)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(take)
            // Окремий SQL-запит для фото: без нього JOIN множив би рядки постів на кількість фото.
            .AsSplitQuery()
            .ToListAsync(ct);
    }

    private static IQueryable<Post> WithDetails(IQueryable<Post> posts) =>
        posts.Include(p => p.Author).Include(p => p.Media);
}
