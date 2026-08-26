using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    /// <summary>
    /// Drives the release publishing workflow:
    /// Draft -> Testing -> Approved -> Published -> Deprecated -> Archived.
    /// Each transition method only succeeds from the expected current status.
    /// </summary>
    public interface IReleaseService {
        Task<ApplicationVersionDTO> SubmitForTestingAsync(string applicationCode, string version);

        Task<ApplicationVersionDTO> ApproveAsync(string applicationCode, string version);

        /// <summary>Publishes a version. Fails if no update package has been uploaded for it yet.</summary>
        Task<ApplicationVersionDTO> PublishAsync(string applicationCode, string version, string? publishedBy);

        Task<ApplicationVersionDTO> DeprecateAsync(string applicationCode, string version);

        Task<ApplicationVersionDTO> ArchiveAsync(string applicationCode, string version);

        Task<IReadOnlyList<ReleaseNoteDTO>> AddReleaseNotesAsync(string applicationCode, string version, IReadOnlyList<CreateReleaseNoteDTO> releaseNotes);
    }
}
