using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IUpdateHistoryRepository {
        Task<IReadOnlyList<UpdateHistory>> GetByApplicationIdAsync(int applicationId);

        Task<int> CountSinceAsync(DateTime sinceUtc);

        Task AddAsync(UpdateHistory history);

        Task SaveChangesAsync();
    }
}
