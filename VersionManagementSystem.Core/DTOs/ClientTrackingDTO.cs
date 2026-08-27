using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Core.DTOs {
    // <summary>Request body for POST /api/update/history — a client reporting the outcome of an update attempt.</summary>
    public sealed class RecordUpdateHistoryDTO {
        public string ApplicationCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
    public sealed class UpdateHistoryDTO {
        public int UpdateHistoryId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public DateTime UpdateDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
    }
    public sealed class ClientInstallationDTO {
        public int ClientInstallationId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string CurrentVersion { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    /// <summary>Aggregate counts for the admin dashboard landing page.</summary>
    public sealed class DashboardSummaryDTO {
        public int Applications { get; set; }
        public int PublishedVersions { get; set; }
        public int PendingReleases { get; set; }
        public int UpdatesToday { get; set; }
    }
}
