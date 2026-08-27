using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VersionManagementSystem.Client {
    public interface IRollbackService {

        /// <summary>Backs up installPath's current files before an update. Returns the backup folder path.</summary>
        Task<string> CreateBackupAsync(string installPath, string applicationCode, string version, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Restores installPath from a previously created backup. Only restores files the backup
        /// manifest recorded — it never touches files the update system didn't back up.
        /// </summary>
        Task RollbackAsync(string installPath, string applicationCode, string version, CancellationToken cancellationToken = default);
    }
}
