using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Core.Models;

namespace VersionManagementSystem.Core.Services {
    public sealed class PackageService : IPackageService {
        private static readonly Dictionary<string, PackageType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase) {
            [".zip"] = PackageType.Zip,
            [".exe"] = PackageType.Exe,
            [".msi"] = PackageType.Msi
        };

        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;
        private readonly IUpdatePackageRepository _packageRepository;
        private readonly IPackageStorageService _storageService;
        private readonly IChecksumService _checksumService;

        public PackageService(IApplicationRepository applicationRepository, IApplicationVersionRepository versionRepository, IUpdatePackageRepository packageRepository, IPackageStorageService storageService, IChecksumService checksumService) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
            _packageRepository = packageRepository;
            _storageService = storageService;
            _checksumService = checksumService;
        }

        public async Task<UpdatePackageDTO> UploadAsync(string applicationCode, string version, string originalFileName, Stream content, string? uploadedBy) {
            var (application, versionEntity) = await GetApplicationAndVersionOrThrowAsync(applicationCode, version);

            var extension = Path.GetExtension(originalFileName);
            if (!ExtensionMap.TryGetValue(extension, out var packageType)) {
                throw new ValidationException($"'{extension}' is not a supported package type. Expected .zip, .exe or .msi.");
            }

            if (await _packageRepository.GetByVersionAndTypeAsync(versionEntity.ApplicationVersionId, packageType) is not null) {
                throw new ValidationException($"A {packageType} package already exists for version {versionEntity.VersionString}. " + "Remove it first if you need to replace it.");
            }

            if (content.Length == 0) {
                throw new ValidationException("The uploaded package file is empty.");
            }

            var (relativePath, fileSize) = await _storageService.SaveAsync(application.ApplicationCode, versionEntity.VersionString, packageType.ToString(), originalFileName, content);

            // Recompute the checksum from the file as actually persisted to disk, not the in-memory upload stream,
            // so the stored checksum always reflects exactly what a client will later download and verify.
            await using var savedStream = await _storageService.OpenReadAsync(relativePath);
            var checksum = await _checksumService.ComputeSha256Async(savedStream);

            var package = new UpdatePackage {
                ApplicationVersionId = versionEntity.ApplicationVersionId,
                FileName = Path.GetFileName(originalFileName),
                FilePath = relativePath,
                FileSize = fileSize,
                Checksum = checksum,
                PackageType = packageType,
                CreatedDate = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            await _packageRepository.AddAsync(package);
            await _packageRepository.SaveChangesAsync();

            return MapToDto(package, application.ApplicationCode, versionEntity.VersionString);
        }

        public async Task<IReadOnlyList<UpdatePackageDTO>> GetByVersionAsync(string applicationCode, string version) {
            var (application, versionEntity) = await GetApplicationAndVersionOrThrowAsync(applicationCode, version);
            var packages = await _packageRepository.GetByApplicationVersionIdAsync(versionEntity.ApplicationVersionId);

            return packages.Select(p => MapToDto(p, application.ApplicationCode, versionEntity.VersionString)).ToList();
        }

        public async Task<(Stream Content, string FileName, string Checksum)> GetDownloadAsync(int updatePackageId) {
            var package = await _packageRepository.GetByIdAsync(updatePackageId);
            if (package is null) {
                throw new NotFoundException($"Package {updatePackageId} was not found.");
            }

            // Access is always by package ID, resolved internally to a storage-relative path —
            // the caller never supplies or sees a filesystem path.
            try {
                var stream = await _storageService.OpenReadAsync(package.FilePath);
                return (stream, package.FileName, package.Checksum);
            }
            catch (FileNotFoundException) {
                throw new NotFoundException($"Package {updatePackageId} is registered but its file is missing from storage.");
            }
        }

        private async Task<(Application Application, ApplicationVersion Version)> GetApplicationAndVersionOrThrowAsync(
            string applicationCode, string version) {
            var application = await _applicationRepository.GetByCodeAsync(applicationCode);
            if (application is null) {
                throw new NotFoundException($"Application '{applicationCode}' was not found.");
            }

            if (!SemanticVersion.TryParse(version, out var semanticVersion) || semanticVersion is null) {
                throw new ValidationException($"'{version}' is not a valid version.");
            }

            var versionEntity = await _versionRepository.GetByVersionAsync(application.ApplicationId, semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch);

            if (versionEntity is null) {
                throw new NotFoundException($"Version {semanticVersion} was not found for application '{applicationCode}'.");
            }

            return (application, versionEntity);
        }

        private static UpdatePackageDTO MapToDto(UpdatePackage package, string applicationCode, string version) {
            return new UpdatePackageDTO {
                UpdatePackageId = package.UpdatePackageId,
                ApplicationVersionId = package.ApplicationVersionId,
                ApplicationCode = applicationCode,
                Version = version,
                FileName = package.FileName,
                FileSize = package.FileSize,
                Checksum = package.Checksum,
                PackageType = package.PackageType.ToString(),
                CreatedDate = package.CreatedDate,
                UploadedBy = package.UploadedBy
            };
        }
    }
}
