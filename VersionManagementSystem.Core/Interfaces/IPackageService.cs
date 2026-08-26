using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IPackageService {
        Task<UpdatePackageDTO> UploadAsync(
            string applicationCode, string version, string originalFileName, Stream content, string? uploadedBy);

        Task<IReadOnlyList<UpdatePackageDTO>> GetByVersionAsync(string applicationCode, string version);

        /// <summary>Resolves an open read stream, file name and checksum for a package by its ID only — never by path.</summary>
        Task<(Stream Content, string FileName, string Checksum)> GetDownloadAsync(int updatePackageId);
    }
}
