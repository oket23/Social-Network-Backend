using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocialNetwork.Domain.Auth;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Infrastructure.Persistence;

/// <summary>
/// IdentityDbContext уже містить таблиці Identity (users, roles, user_claims...).
/// IDataProtectionKeyContext — ключі для токенів email/скидання пароля зберігаються в БД,
/// тож вони не губляться після перезапуску контейнера на сервері.
/// </summary>
public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Підхоплює всі класи IEntityTypeConfiguration з папки Configurations.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
