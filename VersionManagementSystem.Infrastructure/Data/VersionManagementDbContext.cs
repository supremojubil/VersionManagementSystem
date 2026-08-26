using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Infrastructure.Data.Configurations;

namespace VersionManagementSystem.Infrastructure.Data {
    public class VersionManagementDbContext : DbContext {
        public VersionManagementDbContext(DbContextOptions<VersionManagementDbContext> options) : base(options) {

        }

        public DbSet<Application> Applications => Set<Application>();

        public DbSet<ApplicationVersion> ApplicationVersions => Set<ApplicationVersion>();

        public DbSet<ReleaseNote> ReleaseNotes => Set<ReleaseNote>();

        public DbSet<UpdatePackage> UpdatePackages => Set<UpdatePackage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfiguration(new ApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationVersionConfiguration());
            modelBuilder.ApplyConfiguration(new ReleaseNoteConfiguration());
            modelBuilder.ApplyConfiguration(new UpdatePackageConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
