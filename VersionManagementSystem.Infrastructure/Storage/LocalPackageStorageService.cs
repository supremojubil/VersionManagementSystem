using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Infrastructure.Storage {
    /// <summary>
    /// Stores packages on local disk under:
    ///   {RootPath}/{ApplicationCode}/{Version}/{PackageType}/{fileName}
    /// Every path is built from sanitized segments and re-validated to sit inside
    /// the configured root before any file I/O — this is the path-traversal guard.
    /// </summary>
    public sealed class LocalPackageStorageService : IPackageStorageService {
        private readonly string _rootPath;

        public LocalPackageStorageService(IOptions<PackageStorageOptions> options) {
            _rootPath = Path.GetFullPath(options.Value.RootPath);
            Directory.CreateDirectory(_rootPath);
        }

        public async Task<(string RelativePath, long FileSize)> SaveAsync(string applicationCode, string version, string packageTypeFolder, string originalFileName, Stream content, CancellationToken cancellationToken = default) {
            var safeFileName = SanitizeSegment(Path.GetFileName(originalFileName));
            if (string.IsNullOrWhiteSpace(safeFileName)) {
                throw new ArgumentException("A valid file name is required.", nameof(originalFileName));
            }

            var relativePath = Path.Combine(SanitizeSegment(applicationCode), SanitizeSegment(version), SanitizeSegment(packageTypeFolder), safeFileName);

            var fullPath = ResolveAndValidatePath(relativePath);

            var directory = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(directory);

            if (content.CanSeek) {
                content.Seek(0, SeekOrigin.Begin);
            }

            await using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            var fileSize = new FileInfo(fullPath).Length;

            // Normalize to forward slashes so the stored relative path is platform-independent.
            var normalizedRelativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
            return (normalizedRelativePath, fileSize);
        }

        public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) {
            var fullPath = ResolveAndValidatePath(relativePath);

            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException("The requested package file could not be found on disk.", fullPath);
            }

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) {
            var fullPath = ResolveAndValidatePath(relativePath);

            if (File.Exists(fullPath)) {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private static string SanitizeSegment(string segment) {
            if (string.IsNullOrWhiteSpace(segment)) {
                return string.Empty;
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(Array.FindAll(segment.ToCharArray(), c => Array.IndexOf(invalidChars, c) < 0));

            // Defense in depth against path traversal, even though invalid-char stripping
            // above already removes '/' and '\' on all platforms.
            return cleaned.Replace("..", string.Empty).Trim();
        }

        private string ResolveAndValidatePath(string relativePath) {
            var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

            if (!fullPath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase)) {
                throw new UnauthorizedAccessException("Resolved package path is outside the storage root.");
            }

            return fullPath;
        }
    }
}
