using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Auth;
using SocialNetwork.Domain.Auth;
using SocialNetwork.Infrastructure.Persistence.Repositories.Base;

namespace SocialNetwork.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext context) : base(context)
    {
    }

    // Важливо: Set, а не Query (AsNoTracking) — інакше відкликання токена не збережеться
    // (саме така помилка була в JobBoard).
    public Task<RefreshToken?> GetByHash(string tokenHash, CancellationToken ct = default) =>
        Set.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task RevokeAllForUser(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return Set
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }
}
