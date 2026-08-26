using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VersionManagementSystem.Core.Interfaces {
    /// <summary>
    /// Abstracts physical storage of package files. Implementations must never allow
    /// a caller-supplied path to escape the configured storage root (path traversal).
    /// </summary>
    public interface IPackageStorageService {
        /// <summary>
        /// Saves content under a location derived from applicationCode/version/packageType.
        /// Returns the storage-relative path (never an absolute filesystem path) and byte size.
        /// </summary>
        Task<(string RelativePath, long FileSize)> SaveAsync(
            string applicationCode,
            string version,
            string packageTypeFolder,
            string originalFileName,
            Stream content,
            CancellationToken cancellationToken = default);

        /// <summary>Opens a read stream for a previously stored relative path.</summary>
        Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);

        Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    }
}
