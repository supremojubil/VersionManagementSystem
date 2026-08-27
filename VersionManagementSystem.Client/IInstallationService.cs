using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Client {
    public interface IInstallationService {

        /// <summary>Installs a downloaded package into installPath. Returns true on success.</summary>
        Task<bool> InstallAsync(string packageFilePath, PackageType packageType, string installPath, CancellationToken cancellationToken = default);

        /// <summary>Sanity-checks that the install directory looks correctly populated after install.</summary>
        Task<bool> VerifyInstallationAsync(string installPath, CancellationToken cancellationToken = default);
    }
}
