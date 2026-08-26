using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IChecksumService {
        /// <summary>Computes the lowercase hex SHA-256 checksum of a stream's content.</summary>
        Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default);

        /// <summary>Recomputes the checksum of the stream and compares it (case-insensitive) to the expected value.</summary>
        Task<bool> VerifyAsync(Stream content, string expectedChecksum, CancellationToken cancellationToken = default);
    }
}
