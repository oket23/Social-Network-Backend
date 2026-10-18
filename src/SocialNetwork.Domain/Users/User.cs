using Microsoft.AspNetCore.Identity;
using SocialNetwork.Domain.Auth;
using SocialNetwork.Domain.Common;

namespace SocialNetwork.Domain.Users;

/// <summary>
/// Обліковий запис + профіль. Email, UserName, PasswordHash, EmailConfirmed тощо
/// успадковані від IdentityUser — ними керує ASP.NET Core Identity (UserManager).
/// </summary>
public class User : IdentityUser<Guid>, ISoftDeletable
{
    public required string DisplayName { get; set; }

    public string? Bio { get; set; }

    public string? WebsiteUrl { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsPrivate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
