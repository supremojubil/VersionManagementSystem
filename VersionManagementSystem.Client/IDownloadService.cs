using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client.Progress;

namespace VersionManagementSystem.Client {
    public interface IDownloadService {
        /// <summary>
        /// Downloads a package to destinationPath, reporting progress, retrying transient failures,
        /// resuming a partial download where the server supports byte ranges, and verifying the
        /// SHA-256 checksum once the file is complete. Throws PackageDownloadException on failure.
        /// </summary>
        Task DownloadAsync(Uri downloadUrl, string destinationPath, string expectedChecksum, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default);
    }
}
