using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Infrastructure.Data.Configurations {
    public class ClientInstallationConfiguration : IEntityTypeConfiguration<ClientInstallation> {
        public void Configure(EntityTypeBuilder<ClientInstallation> builder) {
            builder.ToTable("ClientInstallations");

            builder.HasKey(c => c.ClientInstallationId);

            builder.Property(c => c.ClientInstallationId).ValueGeneratedOnAdd();

            builder.Property(c => c.MachineName).IsRequired().HasMaxLength(200);

            builder.Property(c => c.CurrentVersion).IsRequired().HasMaxLength(20);

            builder.Property(c => c.LastChecked).IsRequired();

            builder.HasOne(c => c.Application)
                .WithMany()
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // One tracked row per (application, machine).
            builder.HasIndex(c => new { 
                c.ApplicationId, 
                c.MachineName 
            }).IsUnique();
        }
    }
}
