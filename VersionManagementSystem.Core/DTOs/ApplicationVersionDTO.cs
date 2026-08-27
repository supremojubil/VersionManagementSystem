using System;
using System.Collections.Generic;

namespace VersionManagementSystem.Core.DTOs {
    /// <summary>Response contract for a version history entry.</summary>
    public sealed class ApplicationVersionDTO {
        public int ApplicationVersionId { get; set; }
        public int ApplicationId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ReleaseType { get; set; } = string.Empty;
        public string ReleaseStatus { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? MinimumSupportedVersion { get; set; }
        public bool IsMandatory { get; set; }
        public string Channel { get; set; } = "Stable";
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string? PublishedBy { get; set; }
        public List<ReleaseNoteDTO> StructuredReleaseNotes { get; set; } = new();
        public List<UpdatePackageDTO> Packages { get; set; } = new();
    }

    /// <summary>
    /// Request contract for registering a new version. ReleaseType and MinimumSupportedVersion
    /// are strings on purpose: the DTO is a data contract, parsing/validation happens in the service layer.
    /// </summary>
    public sealed class CreateApplicationVersionDTO {
        public string ApplicationCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string ReleaseType { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? MinimumSupportedVersion { get; set; }
        public bool IsMandatory { get; set; }
        public string Channel { get; set; } = "Stable";
        public string? CreatedBy { get; set; }
    }
}
