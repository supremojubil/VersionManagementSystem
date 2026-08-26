using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IApplicationRepository {
        Task<Application?> GetByIdAsync(int applicationId);

        Task<Application?> GetByCodeAsync(string applicationCode);

        Task<IReadOnlyList<Application>> GetAllAsync(bool includeInactive = false);

        Task<bool> CodeExistsAsync(string applicationCode);

        Task AddAsync(Application application);

        Task UpdateAsync(Application application);

        Task SaveChangesAsync();
    }
}
