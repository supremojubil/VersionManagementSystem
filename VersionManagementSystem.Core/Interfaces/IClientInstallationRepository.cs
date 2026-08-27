using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IClientInstallationRepository {
        Task<ClientInstallation?> GetAsync(int applicationId, string machineName);

        Task AddAsync(ClientInstallation installation);

        Task UpdateAsync(ClientInstallation installation);

        Task SaveChangesAsync();
    }
}
