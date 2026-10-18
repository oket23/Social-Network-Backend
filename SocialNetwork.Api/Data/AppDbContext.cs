using Microsoft.EntityFrameworkCore;
using SocialNetwork.Api.Models;
using SocialNetwork.Api.Services;

namespace SocialNetwork.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username).IsRequired().HasMaxLength(30);
            entity.HasIndex(u => u.Username).IsUnique();

            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.PasswordHash).IsRequired();

            entity.Property(u => u.DisplayName).IsRequired().HasMaxLength(50);

            entity.Property(u => u.Bio).HasMaxLength(160);

            entity.Property(u => u.WebsiteUrl).HasMaxLength(200);

            entity.Property(u => u.IsPrivate).IsRequired().HasDefaultValue(false);

            entity.Property(u => u.CreatedAt).IsRequired();
        });

        // Seed-дані для dev-режиму: поки немає реєстрації/логіну (JWT ще не впроваджено),
        // DevCurrentUserService підставляє саме цього користувача як "поточного".
        // Пароль для тестування зміни пароля: Password123!
        // Хеш згенеровано один раз через PasswordHasher<User> і захардкоджено, бо HasData
        // вимагає детермінованих значень (інакше EF щоразу бачить "нову" модель).
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = DevCurrentUserService.DevUserId,
            Username = "dev_user",
            Email = "dev@example.com",
            PasswordHash = "AQAAAAIAAYagAAAAEMMeOSZgsPXX+Qt5s+sszYjeYA4L0BOfnX12K8oXzDCfF1Gw7Siij1QT7J7IFkCpFQ==",
            DisplayName = "Dev User",
            Bio = "Тестовий профіль для розробки кабінету користувача.",
            WebsiteUrl = null,
            AvatarUrl = null,
            IsPrivate = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
