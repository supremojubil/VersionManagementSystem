using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class ApplicationConfiguration : IEntityTypeConfiguration<Application> {
        public void Configure(EntityTypeBuilder<Application> builder) {
            builder.ToTable("Applications");

            builder.HasKey(a => a.ApplicationId);

            // Identity generation belongs here, in the entity/database layer — never in DTOs.
            builder.Property(a => a.ApplicationId).ValueGeneratedOnAdd();

            builder.Property(a => a.ApplicationCode).IsRequired().HasMaxLength(20);

            builder.HasIndex(a => a.ApplicationCode).IsUnique();

            builder.Property(a => a.ApplicationName).IsRequired().HasMaxLength(200);

            builder.Property(a => a.Description).HasMaxLength(1000);

            builder.Property(a => a.IsActive).IsRequired();

            builder.Property(a => a.CreatedDate).IsRequired();

            builder.HasMany(a => a.Versions)
                .WithOne(v => v.Application)
                .HasForeignKey(v => v.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
