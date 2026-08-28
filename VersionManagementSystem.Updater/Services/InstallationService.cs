using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Updater.Services {
    /// <summary>
    /// Installs a downloaded package. Zip packages are extracted directly into installPath
    /// (overwriting existing files). Exe/Msi packages are run as silent installers — the exact
    /// silent-install arguments are installer-specific, so the defaults here (/S for exe,
    /// msiexec /quiet for msi) are a starting point most target apps will need to adjust.
    /// </summary>
    public sealed class InstallationService : IInstallationService {
        public async Task<bool> InstallAsync(string packageFilePath, PackageType packageType, string installPath, CancellationToken cancellationToken = default) {
            if (!File.Exists(packageFilePath)) {
                throw new FileNotFoundException("Package file was not found.", packageFilePath);
            }

            Directory.CreateDirectory(installPath);

            switch (packageType) {
                case PackageType.Zip:
                    return await InstallZipAsync(packageFilePath, installPath, cancellationToken);

                case PackageType.Exe:
                    return await RunInstallerAsync(packageFilePath, "/S", cancellationToken);

                case PackageType.Msi:
                    return await RunInstallerAsync(
                        "msiexec.exe", $"/i \"{packageFilePath}\" /quiet /norestart", cancellationToken);

                default:
                    throw new NotSupportedException($"Package type '{packageType}' is not supported.");
            }
        }

        public Task<bool> VerifyInstallationAsync(string installPath, CancellationToken cancellationToken = default) {
            // Minimal sanity check for Phase 6: the install directory exists and isn't empty.
            // Target applications can extend this (e.g. checking a version marker file or
            // reading their own assembly version) once they wire this library in.
            var isValid = Directory.Exists(installPath) && Directory.EnumerateFileSystemEntries(installPath).Any();
            return Task.FromResult(isValid);
        }

        private static async Task<bool> InstallZipAsync(string zipPath, string installPath, CancellationToken cancellationToken) {
            await Task.Run(() => {
                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var entry in archive.Entries) {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(entry.Name)) {
                        continue; // directory entry
                    }

                    var destinationPath = Path.Combine(installPath, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }, cancellationToken);

            return true;
        }

        private static async Task<bool> RunInstallerAsync(string fileName, string arguments, CancellationToken cancellationToken) {
            var startInfo = new ProcessStartInfo {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start installer process '{fileName}'.");

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
    }
}
