using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IApplicationVersionRepository {
        Task<ApplicationVersion?> GetByIdAsync(int applicationVersionId);

        Task<IReadOnlyList<ApplicationVersion>> GetHistoryAsync(int applicationId);

        Task<ApplicationVersion?> GetLatestAsync(int applicationId);

        /// <summary>Latest version with ReleaseStatus.Published on the given channel  what update-check clients see.</summary>
        Task<ApplicationVersion?> GetLatestPublishedAsync(int applicationId, UpdateChannel channel);

        Task<ApplicationVersion?> GetByVersionAsync(int applicationId, int major, int minor, int patch, int revision);

        Task<bool> VersionExistsAsync(int applicationId, int major, int minor, int patch, int revision);

        Task<int> CountPublishedAsync();

        Task<int> CountPendingAsync();

        Task AddAsync(ApplicationVersion version);

        Task UpdateAsync(ApplicationVersion version);

        Task SaveChangesAsync();
    }
}
