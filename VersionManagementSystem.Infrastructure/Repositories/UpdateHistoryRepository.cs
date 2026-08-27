using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Infrastructure.Data;

namespace VersionManagementSystem.Infrastructure.Repositories {
    public class UpdateHistoryRepository : IUpdateHistoryRepository {
        private readonly VersionManagementDbContext _context;
        public UpdateHistoryRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<IReadOnlyList<UpdateHistory>> GetByApplicationIdAsync(int applicationId) {
            return await _context.UpdateHistories.Where(c => c.ApplicationId == applicationId).OrderByDescending(c => c.UpdateDate).ToListAsync();
        }

        public async Task<int> CountSinceAsync(DateTime sinceUtc) {
            return await _context.UpdateHistories.CountAsync(c => c.UpdateDate >= sinceUtc);
        }

        public async Task AddAsync(UpdateHistory history) {
            await _context.UpdateHistories.AddAsync(history);
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
