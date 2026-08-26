using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IApplicationService {
        Task<IReadOnlyList<ApplicationDTO>> GetAllAsync(bool includeInactive = false);

        Task<ApplicationDTO> GetByCodeAsync(string applicationCode);

        Task<ApplicationDTO> CreateAsync(CreateApplicationDTO request);

        Task<ApplicationDTO> UpdateAsync(string applicationCode, UpdateApplicationDTO request);

        Task DisableAsync(string applicationCode);
    }
}
