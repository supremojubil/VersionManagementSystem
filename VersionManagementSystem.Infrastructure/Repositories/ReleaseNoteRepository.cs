using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Infrastructure.Data;

namespace VersionManagementSystem.Infrastructure.Repositories {
    public class ReleaseNoteRepository : IReleaseNoteRepository {
        private readonly VersionManagementDbContext _context;

        public ReleaseNoteRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<ReleaseNote>> GetByApplicationVersionIdAsync(int applicationVersionId) {
            return await _context.ReleaseNotes.Where(c => c.ApplicationVersionId == applicationVersionId).ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<ReleaseNote> releaseNotes) {
            await _context.ReleaseNotes.AddRangeAsync(releaseNotes);
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
