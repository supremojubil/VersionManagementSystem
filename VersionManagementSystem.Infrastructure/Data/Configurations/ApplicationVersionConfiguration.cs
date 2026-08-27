using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class ApplicationVersionConfiguration : IEntityTypeConfiguration<ApplicationVersion> {
        public void Configure(EntityTypeBuilder<ApplicationVersion> builder) {
            builder.ToTable("ApplicationVersions");

            builder.HasKey(v => v.ApplicationVersionId);

            builder.Property(v => v.ApplicationVersionId).ValueGeneratedOnAdd();

            builder.Property(v => v.Major).IsRequired();
            builder.Property(v => v.Minor).IsRequired();
            builder.Property(v => v.Patch).IsRequired();

            builder.Property(v => v.ReleaseType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(v => v.ReleaseStatus)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(v => v.ReleaseDate).IsRequired();

            builder.Property(v => v.ReleaseNotes).HasMaxLength(4000);

            builder.Property(v => v.MinimumSupportedVersion).HasMaxLength(20);

            builder.Property(v => v.IsMandatory).IsRequired();

            builder.Property(v => v.CreatedDate).IsRequired();

            builder.Property(v => v.CreatedBy).HasMaxLength(200);

            builder.Property(v => v.PublishedBy).HasMaxLength(200);

            builder.Property(v => v.Channel)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // One application cannot register the same MAJOR.MINOR.PATCH twice.
            builder.HasIndex(v => new { 
                v.ApplicationId, 
                v.Major,
                v.Minor,
                v.Patch 
            }).IsUnique();
        }
    }
}
