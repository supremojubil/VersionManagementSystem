using System;

namespace VersionManagementSystem.Core.DTOs {
    /// <summary>Response contract for an application. No ID-generation logic lives here.</summary>
    public sealed class ApplicationDTO {
        public int ApplicationId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? CurrentVersion { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public sealed class CreateApplicationDTO {
        public string ApplicationCode { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class UpdateApplicationDTO {
        public string ApplicationName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
