using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.Domain.Posts;

namespace SocialNetwork.Infrastructure.Persistence.Configurations.Posts;

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("post_media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Url).HasMaxLength(500).IsRequired();

        builder.HasIndex(m => new { m.PostId, m.Position }).IsUnique();

        // Узгоджено з фільтром на Post: фото видаленого поста теж не видно.
        builder.HasQueryFilter(m => m.Post.DeletedAt == null && m.Post.Author.DeletedAt == null);
    }
}
