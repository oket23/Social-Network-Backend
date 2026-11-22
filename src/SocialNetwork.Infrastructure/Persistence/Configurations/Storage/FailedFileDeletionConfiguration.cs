using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetwork.Domain.Storage;

namespace SocialNetwork.Infrastructure.Persistence.Configurations.Storage;

public class FailedFileDeletionConfiguration : IEntityTypeConfiguration<FailedFileDeletion>
{
    public void Configure(EntityTypeBuilder<FailedFileDeletion> builder)
    {
        builder.ToTable("failed_file_deletions");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Url).HasMaxLength(500).IsRequired();
        builder.Property(f => f.LastError).HasMaxLength(1000);

        // Фонова служба на кожному проході шукає рядки з NextAttemptAt у минулому.
        builder.HasIndex(f => f.NextAttemptAt);
    }
}
