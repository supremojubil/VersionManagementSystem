using System.Collections.Generic;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IUpdatePackageRepository {
        Task<UpdatePackage?> GetByIdAsync(int updatePackageId);

        Task<IReadOnlyList<UpdatePackage>> GetByApplicationVersionIdAsync(int applicationVersionId);

        Task<UpdatePackage?> GetByVersionAndTypeAsync(int applicationVersionId, PackageType packageType);

        Task<bool> AnyForVersionAsync(int applicationVersionId);

        Task AddAsync(UpdatePackage package);

        Task SaveChangesAsync();
    }
}
