using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Application.Users;
using SocialNetwork.Domain.Users;
using SocialNetwork.Infrastructure.Persistence.Repositories.Base;

namespace SocialNetwork.Infrastructure.Persistence.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    private readonly ILookupNormalizer _normalizer;

    public UserRepository(AppDbContext context, ILookupNormalizer normalizer) : base(context)
    {
        _normalizer = normalizer;
    }

    public Task<bool> IsUsernameTaken(string username, Guid exceptUserId, CancellationToken ct = default)
    {
        // Порівнюємо нормалізовані (UPPERCASE) значення — так само, як це робить Identity.
        var normalized = _normalizer.NormalizeName(username);
        return Query.AnyAsync(u => u.Id != exceptUserId && u.NormalizedUserName == normalized, ct);
    }
}
