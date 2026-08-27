using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class UpdateHistoryConfiguration : IEntityTypeConfiguration<UpdateHistory> {
        public void Configure(EntityTypeBuilder<UpdateHistory> builder) {
            builder.ToTable("UpdateHistories");

            builder.HasKey(h => h.UpdateHistoryId);

            builder.Property(h => h.UpdateHistoryId).ValueGeneratedOnAdd();

            builder.Property(h => h.FromVersion).IsRequired().HasMaxLength(20);

            builder.Property(h => h.ToVersion).IsRequired().HasMaxLength(20);

            builder.Property(h => h.UpdateDate).IsRequired();

            builder.Property(h => h.Status).IsRequired().HasMaxLength(50);

            builder.Property(h => h.MachineName).IsRequired().HasMaxLength(200);

            builder.HasOne(h => h.Application)
                .WithMany()
                .HasForeignKey(h => h.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(h => new { 
                h.ApplicationId, 
                h.UpdateDate 
            });
        }
    }
}
