using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            Directory.CreateDirectory(installPath);

            foreach (var relativePath in manifest) {
                cancellationToken.ThrowIfCancellationRequested();

                var sourceFile = Path.Combine(backupFolder, relativePath);
                var destinationFile = Path.Combine(installPath, relativePath);

                if (!File.Exists(sourceFile)) {
                    continue; // manifest entry without a matching backup file — skip rather than fail the whole rollback
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                File.Copy(sourceFile, destinationFile, overwrite: true);
            }
        }
    }
}
