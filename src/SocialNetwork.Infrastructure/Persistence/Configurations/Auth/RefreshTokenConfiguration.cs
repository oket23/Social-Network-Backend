using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.Domain.Auth;

namespace SocialNetwork.Infrastructure.Persistence.Configurations.Auth;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        // SHA-256 у hex — рівно 64 символи.
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(64);

        builder.HasIndex(t => t.UserId);

        // Оптимістична конкурентність через системну колонку PostgreSQL xmin:
        // якщо два запити одночасно ротують той самий токен, другий отримає помилку, а не дубль.
        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Токени "видалених" користувачів теж не видно (узгоджено з фільтром на User).
        builder.HasQueryFilter(t => t.User.DeletedAt == null);
    }
}
