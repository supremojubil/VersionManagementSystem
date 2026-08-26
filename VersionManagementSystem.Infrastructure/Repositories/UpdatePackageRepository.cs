using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Infrastructure.Data;

namespace VersionManagementSystem.Infrastructure.Repositories {
    public class UpdatePackageRepository : IUpdatePackageRepository {
        private readonly VersionManagementDbContext _context;

        public UpdatePackageRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<UpdatePackage?> GetByIdAsync(int updatePackageId) {
            return await _context.UpdatePackages.FirstOrDefaultAsync(c => c.UpdatePackageId == updatePackageId);
        }

        public async Task<IReadOnlyList<UpdatePackage>> GetByApplicationVersionIdAsync(int applicationVersionId) {
            return await _context.UpdatePackages.Where(c => c.ApplicationVersionId == applicationVersionId).ToListAsync();
        }

        public async Task<UpdatePackage?> GetByVersionAndTypeAsync(int applicationVersionId, PackageType packageType) {
            return await _context.UpdatePackages.FirstOrDefaultAsync(c => c.ApplicationVersionId == applicationVersionId && c.PackageType == packageType);
        }

        public async Task<bool> AnyForVersionAsync(int applicationVersionId) {
            return await _context.UpdatePackages.AnyAsync(c => c.ApplicationVersionId == applicationVersionId);
        }

        public async Task AddAsync(UpdatePackage package) {
            await _context.UpdatePackages.AddAsync(package);
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
