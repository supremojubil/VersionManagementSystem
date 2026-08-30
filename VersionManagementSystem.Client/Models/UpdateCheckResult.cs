using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Client.Models {
    /// <summary>
    /// Client-side mirror of the server's UpdateCheckResultDto (GET /api/update/check).
    /// Kept independent from Core.DTOs on purpose: target applications reference only this
    /// Client library, never the server's Core/Infrastructure projects.
    /// </summary>
    public sealed class UpdateCheckResult {
        public bool UpdateAvailable { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string? ReleaseType { get; set; }
        public string? DownloadUrl { get; set; }
        public string? Checksum { get; set; }
        public string? PackageType { get; set; }
        public long? FileSize { get; set; }
        public string? ReleaseNotes { get; set; }
        public bool Mandatory { get; set; }
    }
}
