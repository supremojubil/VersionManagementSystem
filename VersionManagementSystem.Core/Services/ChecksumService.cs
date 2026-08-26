using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class ChecksumService : IChecksumService {
        public async Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default) {
            if (content.CanSeek) {
                content.Seek(0, SeekOrigin.Begin);
            }

            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(content, cancellationToken);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public async Task<bool> VerifyAsync(Stream content, string expectedChecksum, CancellationToken cancellationToken = default) {
            var actual = await ComputeSha256Async(content, cancellationToken);
            return string.Equals(actual, expectedChecksum, StringComparison.OrdinalIgnoreCase);
        }
    }
}
