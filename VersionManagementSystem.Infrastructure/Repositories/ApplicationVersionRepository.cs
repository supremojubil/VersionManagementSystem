using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Infrastructure.Data;

namespace VersionManagementSystem.Infrastructure.Repositories {
    public class ApplicationVersionRepository : IApplicationVersionRepository {
        private readonly VersionManagementDbContext _context;

        public ApplicationVersionRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<ApplicationVersion?> GetByIdAsync(int applicationVersionId) {
            return await _context.ApplicationVersions.FirstOrDefaultAsync(c => c.ApplicationVersionId == applicationVersionId);
        }

        public async Task<IReadOnlyList<ApplicationVersion>> GetHistoryAsync(int applicationId) {
            return await _context.ApplicationVersions.Where(c => c.ApplicationId == applicationId).ToListAsync();
        }

        public async Task<ApplicationVersion?> GetLatestAsync(int applicationId) {
            return await _context.ApplicationVersions.Where(c => c.ApplicationId == applicationId)
                .OrderByDescending(c => c.Major)
                .ThenByDescending(c => c.Minor)
                .ThenByDescending(c => c.Patch)
                .FirstOrDefaultAsync();
        }

        public async Task<ApplicationVersion?> GetByVersionAsync(int applicationId, int major, int minor, int patch) {
            return await _context.ApplicationVersions.FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.Major == major && c.Minor == minor && c.Patch == patch);
        }

        public async Task<bool> VersionExistsAsync(int applicationId, int major, int minor, int patch) {
            return await _context.ApplicationVersions.AnyAsync(c => c.ApplicationId == applicationId && c.Major == major && c.Minor == minor && c.Patch == patch);
        }

        public async Task AddAsync(ApplicationVersion version) {
            await _context.ApplicationVersions.AddAsync(version);
        }

        public Task UpdateAsync(ApplicationVersion version) {
            _context.ApplicationVersions.Update(version);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
