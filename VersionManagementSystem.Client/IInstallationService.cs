using System;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Client {
    public interface IInstallationService {
        Task<bool> InstallAsync(string packageFilePath, PackageType packageType, string installPath, CancellationToken cancellationToken = default);

        /// <summary>Verifies that the installed main executable has the expected AssemblyVersion.</summary>
        Task<bool> VerifyInstallationAsync(string installPath, string mainExePath, string expectedVersion, CancellationToken cancellationToken = default);
    }
}
