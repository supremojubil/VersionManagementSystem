using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IApplicationVersionService {
        Task<IReadOnlyList<ApplicationVersionDTO>> GetHistoryAsync(string applicationCode);

        Task<ApplicationVersionDTO> GetLatestAsync(string applicationCode);

        Task<ApplicationVersionDTO> CreateAsync(CreateApplicationVersionDTO request);
    }
}
