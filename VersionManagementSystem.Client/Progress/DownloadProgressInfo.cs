using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Client.Progress {
    public sealed class DownloadProgressInfo {
        public long BytesReceived { get; init; }

        /// <summary>Null when the server didn't report Content-Length (progress % can't be computed).</summary>
        public long? TotalBytes { get; init; }

        public double? PercentComplete => TotalBytes is > 0 ? Math.Round(BytesReceived * 100.0 / TotalBytes.Value, 1) : null;
    }
}
