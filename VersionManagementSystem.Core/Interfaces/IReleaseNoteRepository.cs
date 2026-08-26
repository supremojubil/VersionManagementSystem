using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IReleaseNoteRepository {
        Task<IReadOnlyList<ReleaseNote>> GetByApplicationVersionIdAsync(int applicationVersionId);

        Task AddRangeAsync(IEnumerable<ReleaseNote> releaseNotes);

        Task SaveChangesAsync();
    }
}
