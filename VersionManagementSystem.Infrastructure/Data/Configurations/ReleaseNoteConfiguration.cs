using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class ReleaseNoteConfiguration : IEntityTypeConfiguration<ReleaseNote> {
        public void Configure(EntityTypeBuilder<ReleaseNote> builder) {
            builder.ToTable("ReleaseNotes");

            builder.HasKey(n => n.ReleaseNoteId);

            builder.Property(n => n.ReleaseNoteId).ValueGeneratedOnAdd();

            builder.Property(n => n.Category)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(n => n.Description).IsRequired().HasMaxLength(1000);

            builder.Property(n => n.SortOrder).IsRequired();

            builder.Property(n => n.CreatedDate).IsRequired();

            builder.HasOne(n => n.ApplicationVersion)
                .WithMany(v => v.StructuredReleaseNotes)
                .HasForeignKey(n => n.ApplicationVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
