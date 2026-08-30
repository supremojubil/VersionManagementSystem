using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client;

namespace VersionManagementSystem.Updater.Services {
    public sealed class RollbackService : IRollbackService {
        private const string ManifestFileName = "__manifest.json";
        private readonly string _backupRoot;

        public RollbackService(string backupRoot = "Backups") {
            _backupRoot = Path.GetFullPath(backupRoot);
        }

        public async Task<string> CreateBackupAsync(string installPath, string applicationCode, string version, CancellationToken cancellationToken = default) {
            if (!Directory.Exists(installPath)) {
                throw new DirectoryNotFoundException($"Install path '{installPath}' does not exist — nothing to back up.");
            }

            var backupFolder = Path.Combine(_backupRoot, applicationCode, version);
            Directory.CreateDirectory(backupFolder);

            var manifest = new List<string>();
            var installFullPath = Path.GetFullPath(installPath);

            foreach (var sourceFile in Directory.EnumerateFiles(installFullPath, "*", SearchOption.AllDirectories)) {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(installFullPath, sourceFile);
                var destinationFile = Path.Combine(backupFolder, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile, overwrite: true);
                manifest.Add(relativePath);
            }

            var manifestPath = Path.Combine(backupFolder, ManifestFileName);
            var manifestJson = JsonSerializer.Serialize(manifest);
            await File.WriteAllTextAsync(manifestPath, manifestJson, cancellationToken);
            return backupFolder;
        }

        public async Task RollbackAsync(string installPath, string applicationCode, string version, CancellationToken cancellationToken = default) {
            var backupFolder = Path.Combine(_backupRoot, applicationCode, version);
            var manifestPath = Path.Combine(backupFolder, ManifestFileName);

            if (!File.Exists(manifestPath)) {
                throw new FileNotFoundException($"No backup manifest found for {applicationCode} {version} — cannot roll back safely.", manifestPath);
            }

            var manifestJson = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<List<string>>(manifestJson) ?? new List<string>();
            var allowed = new HashSet<string>(manifest.Select(NormalizeRelativePath), StringComparer.OrdinalIgnoreCase);
            var installFullPath = Path.GetFullPath(installPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Remove files created by the failed/new version that did not exist in the backup.
            foreach (var currentFile in Directory.Exists(installPath)
                ? Directory.EnumerateFiles(installPath, "*", SearchOption.AllDirectories).ToList()
                : new List<string>()) {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = Path.GetRelativePath(installPath, currentFile);
                if (!allowed.Contains(NormalizeRelativePath(relativePath))) {
                    File.Delete(currentFile);
                }
            }

            foreach (var relativePath in manifest) {
                cancellationToken.ThrowIfCancellationRequested();

                var normalized = NormalizeRelativePath(relativePath);
                var destinationFile = Path.GetFullPath(Path.Combine(installPath, normalized));
                if (!destinationFile.StartsWith(installFullPath, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidDataException($"Backup manifest contains an unsafe path '{relativePath}'.");
                }

                var sourceFile = Path.GetFullPath(Path.Combine(backupFolder, normalized));
                var backupFullPath = Path.GetFullPath(backupFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!sourceFile.StartsWith(backupFullPath, StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidDataException($"Backup manifest contains an unsafe path '{relativePath}'.");
                }

                if (!File.Exists(sourceFile)) {
                    throw new FileNotFoundException($"Backup file for '{relativePath}' is missing.", sourceFile);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile, overwrite: true);
            }
        }

        private static string NormalizeRelativePath(string path) => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }
}
