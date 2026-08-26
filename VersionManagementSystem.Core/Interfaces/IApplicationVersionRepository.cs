using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IApplicationVersionRepository {
        Task<ApplicationVersion?> GetByIdAsync(int applicationVersionId);

        Task<IReadOnlyList<ApplicationVersion>> GetHistoryAsync(int applicationId);

        Task<ApplicationVersion?> GetLatestAsync(int applicationId);

        Task<ApplicationVersion?> GetByVersionAsync(int applicationId, int major, int minor, int patch);

        Task<bool> VersionExistsAsync(int applicationId, int major, int minor, int patch);

        Task AddAsync(ApplicationVersion version);

        Task UpdateAsync(ApplicationVersion version);

        Task SaveChangesAsync();
    }
}
