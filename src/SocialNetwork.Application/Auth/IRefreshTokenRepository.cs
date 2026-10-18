using SocialNetwork.Application.Common.Abstractions;
using SocialNetwork.Domain.Auth;

namespace SocialNetwork.Application.Auth;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>Шукає токен за хешем разом з користувачем. Сутність відстежується EF (щоб зберегти відкликання).</summary>
    Task<RefreshToken?> GetByHash(string tokenHash, CancellationToken ct = default);

    /// <summary>Відкликає всі активні токени користувача одним UPDATE-запитом.</summary>
    Task RevokeAllForUser(Guid userId, CancellationToken ct = default);
}
