using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class UpdatePackageConfiguration : IEntityTypeConfiguration<UpdatePackage> {
        public void Configure(EntityTypeBuilder<UpdatePackage> builder) {
            builder.ToTable("UpdatePackages");

            builder.HasKey(p => p.UpdatePackageId);

            builder.Property(p => p.UpdatePackageId).ValueGeneratedOnAdd();

            builder.Property(p => p.FileName).IsRequired().HasMaxLength(260);

            builder.Property(p => p.FilePath).IsRequired().HasMaxLength(500);

            builder.Property(p => p.FileSize).IsRequired();

            builder.Property(p => p.Checksum).IsRequired().HasMaxLength(64); // SHA-256 hex string is always 64 characters.

            builder.Property(p => p.PackageType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(p => p.CreatedDate).IsRequired();

            builder.Property(p => p.UploadedBy).HasMaxLength(200);

            builder.HasOne(p => p.ApplicationVersion)
                .WithMany(v => v.Packages)
                .HasForeignKey(p => p.ApplicationVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Only one package per package type (zip/exe/msi) per version.
            builder.HasIndex(p => new { 
                p.ApplicationVersionId,
                p.PackageType 
            }).IsUnique();
        }
    }
}
