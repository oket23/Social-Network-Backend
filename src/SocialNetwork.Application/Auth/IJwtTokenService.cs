using SocialNetwork.Domain.Users;

namespace SocialNetwork.Application.Auth;

public interface IJwtTokenService
{
    /// <summary>Короткоживучий access-токен (JWT), який фронт надсилає в заголовку Authorization.</summary>
    (string Token, DateTime ExpiresAt) CreateAccessToken(User user);

    /// <summary>Випадковий рядок для refresh-токена (не JWT).</summary>
    string GenerateRefreshToken();

    /// <summary>SHA-256 хеш токена — саме він зберігається в БД.</summary>
    string HashToken(string token);
}
