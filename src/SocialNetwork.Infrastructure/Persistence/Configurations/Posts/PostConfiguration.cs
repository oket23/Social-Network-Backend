using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.Domain.Posts;

namespace SocialNetwork.Infrastructure.Persistence.Configurations.Posts;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Caption).HasMaxLength(2200);

        builder.HasOne(p => p.Author)
            .WithMany()
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Media)
            .WithOne(m => m.Post)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Стрічка й профіль сортують за датою (новіші зверху) — індекси під ці запити.
        builder.HasIndex(p => p.CreatedAt).IsDescending();
        builder.HasIndex(p => new { p.AuthorId, p.CreatedAt }).IsDescending(false, true);

        // Soft delete: видалені пости й пости видалених користувачів не потрапляють у жодний запит.
        builder.HasQueryFilter(p => p.DeletedAt == null && p.Author.DeletedAt == null);
    }
}
