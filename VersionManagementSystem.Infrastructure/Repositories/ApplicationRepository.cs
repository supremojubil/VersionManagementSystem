using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Infrastructure.Data;

namespace VersionManagementSystem.Infrastructure.Repositories {
    public class ApplicationRepository : IApplicationRepository {
        private readonly VersionManagementDbContext _context;

        public ApplicationRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<Application?> GetByIdAsync(int applicationId) {
            return await _context.Applications.FirstOrDefaultAsync(c => c.ApplicationId == applicationId);
        }

        public async Task<Application?> GetByCodeAsync(string applicationCode) {
            var normalizedCode = applicationCode.Trim().ToUpperInvariant();

            return await _context.Applications.FirstOrDefaultAsync(c => c.ApplicationCode == normalizedCode);
        }

        public async Task<IReadOnlyList<Application>> GetAllAsync(bool includeInactive = false) {
            var query = _context.Applications.AsQueryable();

            if (!includeInactive) {
                query = query.Where(c => c.IsActive);
            }

            return await query.OrderBy(c => c.ApplicationName).ToListAsync();
        }

        public async Task<bool> CodeExistsAsync(string applicationCode) {
            var normalizedCode = applicationCode.Trim().ToUpperInvariant();

            return await _context.Applications.AnyAsync(c => c.ApplicationCode == normalizedCode);
        }

        public async Task AddAsync(Application application) {
            await _context.Applications.AddAsync(application);
        }

        public Task UpdateAsync(Application application) {
            _context.Applications.Update(application);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
