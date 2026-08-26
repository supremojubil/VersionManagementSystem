using System;

namespace VersionManagementSystem.Core.DTOs
{
    /// <summary>
    /// Response contract for an uploaded package. Deliberately excludes FilePath —
    /// clients only ever get a package Id to download through, never a server path.
    /// </summary>
    public sealed class UpdatePackageDTO
    {
        public int UpdatePackageId { get; set; }
        public int ApplicationVersionId { get; set; }
        public string ApplicationCode { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string Checksum { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UploadedBy { get; set; }
    }
}
