using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Core.DTOs {
    /// <summary>
    /// Response contract for GET /api/update/check and /api/update/latest.
    /// DownloadUrl is a path relative to the API root (e.g. "/api/packages/42/download") —
    /// clients that already know their base URL can resolve it themselves.
    /// </summary>
    public sealed class UpdateCheckResultDTO {
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
