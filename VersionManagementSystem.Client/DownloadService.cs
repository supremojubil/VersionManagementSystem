using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client.Exceptions;
using VersionManagementSystem.Client.Progress;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Core.Services;

namespace VersionManagementSystem.Client {
    // <summary>
    /// Downloads an update package to a temporary ".download" file next to the destination,
    /// verifies its SHA-256 checksum, then atomically moves it into place. Retries transient
    /// failures and resumes via HTTP Range requests when the server supports it (the packages
    /// API's FileStreamResult does, since it serves a plain file through ASP.NET Core).
    /// </summary>
    public sealed class DownloadService : IDownloadService, IDisposable {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly IChecksumService _checksumService;
        private readonly int _maxAttempts;
        private readonly TimeSpan _retryDelay;
        public DownloadService(HttpClient? httpClient = null, int maxAttempts = 3, TimeSpan? retryDelay = null) {
            if (httpClient is not null) {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else {
                _httpClient = new HttpClient();
                _ownsHttpClient = true;
            }

            _checksumService = new ChecksumService();
            _maxAttempts = Math.Max(1, maxAttempts);
            _retryDelay = retryDelay ?? TimeSpan.FromSeconds(2);
        }

        public async Task DownloadAsync(Uri downloadUrl, string destinationPath, string expectedChecksum, IProgress<DownloadProgressInfo>? progress = null, CancellationToken cancellationToken = default) {
            var directory = Path.GetDirectoryName(Path.GetFullPath(destinationPath));
            if (!string.IsNullOrEmpty(directory)) {
                Directory.CreateDirectory(directory);
            }

            var tempPath = destinationPath + ".download";
            Exception? lastError = null;

            for (var attempt = 1; attempt <= _maxAttempts; attempt++) {
                cancellationToken.ThrowIfCancellationRequested();

                try {
                    await DownloadAttemptAsync(downloadUrl, tempPath, progress, cancellationToken);

                    bool checksumValid;
                    await using (var verifyStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        checksumValid = await _checksumService.VerifyAsync(verifyStream, expectedChecksum, cancellationToken);
                    }

                    if (!checksumValid) {
                        // Corrupted download — remove it so the next attempt starts clean rather
                        // than "resuming" from bytes that already failed verification.
                        SafeDelete(tempPath);
                        throw new PackageDownloadException("Downloaded package failed SHA-256 checksum verification (the file may be corrupted).");
                    }

                    if (File.Exists(destinationPath)) {
                        File.Delete(destinationPath);
                    }

                    File.Move(tempPath, destinationPath);
                    return;
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception ex) {
                    lastError = ex;
                    if (attempt == _maxAttempts) {
                        break;
                    }

                    await Task.Delay(_retryDelay, cancellationToken);
                }
            }

            SafeDelete(tempPath);
            throw new PackageDownloadException($"Failed to download package after {_maxAttempts} attempt(s).", lastError ?? new Exception("Unknown error."));
        }

        private async Task DownloadAttemptAsync(Uri downloadUrl, string tempPath, IProgress<DownloadProgressInfo>? progress, CancellationToken cancellationToken) {
            var existingLength = File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0L;

            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            if (existingLength > 0) {
                request.Headers.Range = new RangeHeaderValue(existingLength, null);
            }

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var isResuming = existingLength > 0 && response.StatusCode == HttpStatusCode.PartialContent;
            if (existingLength > 0 && !isResuming) {
                // Server didn't honor the range request (e.g. no Range support) — start over.
                existingLength = 0;
                SafeDelete(tempPath);
            }

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value + existingLength : (long?)null;

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(tempPath, isResuming ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            var totalRead = existingLength;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0) {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;
                progress?.Report(new DownloadProgressInfo { BytesReceived = totalRead, TotalBytes = totalBytes });
            }
        }

        private static void SafeDelete(string path) {
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
            catch {
                // Best-effort cleanup — a leftover temp file doesn't block the next attempt from overwriting it.
            }
        }

        public void Dispose() {
            if (_ownsHttpClient) {
                _httpClient.Dispose();
            }
        }
    }
}
