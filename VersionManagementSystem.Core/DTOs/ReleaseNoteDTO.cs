using System;

namespace VersionManagementSystem.Core.DTOs {
    public sealed class ReleaseNoteDTO {
        public int ReleaseNoteId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public sealed class CreateReleaseNoteDTO {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
