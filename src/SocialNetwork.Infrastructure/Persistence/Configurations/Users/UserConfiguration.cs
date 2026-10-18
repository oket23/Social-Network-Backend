using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.Domain.Users;

namespace SocialNetwork.Infrastructure.Persistence.Configurations.Users;

/// <summary>Обмеження полів — з docs/user-profile-design.md.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.UserName).HasMaxLength(30).IsRequired();
        builder.Property(u => u.NormalizedUserName).HasMaxLength(30);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.Bio).HasMaxLength(160);
        builder.Property(u => u.WebsiteUrl).HasMaxLength(200);
        builder.Property(u => u.AvatarUrl).HasMaxLength(500);
        builder.Property(u => u.IsPrivate).HasDefaultValue(false);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete: "видалені" користувачі не потрапляють у жодний запит.
        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
