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
    public class ClientInstallationRepository : IClientInstallationRepository {
        private readonly VersionManagementDbContext _context;
        public ClientInstallationRepository(VersionManagementDbContext context) {
            _context = context;
        }

        public async Task<ClientInstallation?> GetAsync(int applicationId, string machineName) {
            return await _context.ClientInstallations.FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.MachineName == machineName);
        }

        public async Task AddAsync(ClientInstallation installation) {
            await _context.ClientInstallations.AddAsync(installation);
        }

        public Task UpdateAsync(ClientInstallation installation) {
            _context.ClientInstallations.Update(installation);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync() {
            await _context.SaveChangesAsync();
        }
    }
}
