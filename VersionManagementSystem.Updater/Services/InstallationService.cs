using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Updater.Services {
    /// <summary>Installs update packages and verifies the target executable's AssemblyVersion.</summary>
    public sealed class InstallationService : IInstallationService {
        public async Task<bool> InstallAsync(string packageFilePath, PackageType packageType, string installPath, CancellationToken cancellationToken = default) {
            if (!File.Exists(packageFilePath)) {
                throw new FileNotFoundException("Package file was not found.", packageFilePath);
            }

            Directory.CreateDirectory(installPath);

            return packageType switch {
                PackageType.Zip => await InstallZipAsync(packageFilePath, installPath, cancellationToken),
                PackageType.Exe => await RunInstallerAsync(packageFilePath, "/S", cancellationToken),
                PackageType.Msi => await RunInstallerAsync("msiexec.exe", $"/i \"{packageFilePath}\" /quiet /norestart", cancellationToken),
                _ => throw new NotSupportedException($"Package type '{packageType}' is not supported.")
            };
        }

        public Task<bool> VerifyInstallationAsync(string installPath, string mainExePath, string expectedVersion, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(installPath)) {
                return Task.FromResult(false);
            }

            var fullInstallPath = Path.GetFullPath(installPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullExePath = Path.GetFullPath(mainExePath);

            // The verified executable must be inside the application installation directory.
            if (!fullExePath.StartsWith(fullInstallPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullExePath)) {
                return Task.FromResult(false);
            }

            if (!Version.TryParse(expectedVersion, out var expected) || expected is null || expected.Build < 0 || expected.Revision < 0) {
                return Task.FromResult(false);
            }

            try {
                var actual = AssemblyName.GetAssemblyName(fullExePath).Version;
                var valid = actual is not null && actual.Build >= 0 && actual.Revision >= 0 && actual.Equals(expected);
                return Task.FromResult(valid);
            }
            catch (Exception) {
                return Task.FromResult(false);
            }
        }

        private static async Task<bool> InstallZipAsync(string zipPath, string installPath, CancellationToken cancellationToken) {
            await Task.Run(() => {
                var fullInstallPath = Path.GetFullPath(installPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                using var archive = ZipFile.OpenRead(zipPath);
                foreach (var entry in archive.Entries) {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(entry.Name)) {
                        continue;
                    }

                    var destinationPath = Path.GetFullPath(Path.Combine(installPath, entry.FullName));
                    if (!destinationPath.StartsWith(fullInstallPath, StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidDataException($"ZIP entry '{entry.FullName}' would extract outside the installation directory.");
                    }

                    var directory = Path.GetDirectoryName(destinationPath);
                    if (string.IsNullOrWhiteSpace(directory)) {
                        throw new InvalidDataException($"ZIP entry '{entry.FullName}' has an invalid destination path.");
                    }

                    Directory.CreateDirectory(directory);
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
