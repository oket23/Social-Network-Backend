using SocialNetwork.Domain.Common;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Domain.Auth;

/// <summary>
/// Refresh-токен. У БД зберігається лише SHA-256 хеш, а не сам токен:
/// якщо базу вкрадуть, хешами не можна скористатися.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    /// <summary>Хеш токена, який замінив цей під час ротації (для аудиту ланцюжка).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt is not null;

    public bool IsActive => !IsRevoked && !IsExpired;
}
