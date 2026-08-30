using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class UpdateCheckService : IUpdateCheckService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;
        private readonly IUpdatePackageRepository _packageRepository;
        private readonly IVersionService _versionService;
        private readonly IClientTrackingService _clientTrackingService;

        public UpdateCheckService(IApplicationRepository applicationRepository, IApplicationVersionRepository versionRepository, IUpdatePackageRepository packageRepository,
                                  IVersionService versionService, IClientTrackingService clientTrackingService) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
            _packageRepository = packageRepository;
            _versionService = versionService;
            _clientTrackingService = clientTrackingService;
        }

        public async Task<UpdateCheckResultDTO> CheckForUpdateAsync(string applicationCode, string currentVersion, UpdateChannel channel, string? machineName) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var current = _versionService.Parse(currentVersion);

            if (!string.IsNullOrWhiteSpace(machineName)) {
                await _clientTrackingService.RecordCheckInAsync(applicationCode, machineName, current.ToString(4));
            }

            var latestPublished = await _versionRepository.GetLatestPublishedAsync(application.ApplicationId, channel);

            if (latestPublished is null) {
                return new UpdateCheckResultDTO {
                    UpdateAvailable = false,
                    CurrentVersion = current.ToString(4),
                    LatestVersion = current.ToString(4),
                    Mandatory = false
                };
            }

            var latest = new Version(latestPublished.Major, latestPublished.Minor, latestPublished.Patch, latestPublished.Revision);
            var updateAvailable = current.CompareTo(latest) < 0;
            var mandatory = latestPublished.IsMandatory;

            if (!mandatory && !string.IsNullOrWhiteSpace(latestPublished.MinimumSupportedVersion) &&
                _versionService.TryParse(latestPublished.MinimumSupportedVersion, out var minSupported) && minSupported is not null) {
                mandatory = current.CompareTo(minSupported) < 0;
            }

            var package = updateAvailable ? await GetPreferredPackageAsync(latestPublished) : null;

            return new UpdateCheckResultDTO {
                UpdateAvailable = updateAvailable,
                CurrentVersion = current.ToString(4),
                LatestVersion = latest.ToString(4),
                ReleaseType = latestPublished.ReleaseType.ToString(),
                DownloadUrl = package is null ? null : $"/api/packages/{package.UpdatePackageId}/download",
                Checksum = package?.Checksum,
                PackageType = package?.PackageType.ToString(),
                FileSize = package?.FileSize,
                ReleaseNotes = latestPublished.ReleaseNotes,
                Mandatory = mandatory
            };
        }

        public async Task<UpdateCheckResultDTO> GetLatestAsync(string applicationCode, UpdateChannel channel) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var latestPublished = await _versionRepository.GetLatestPublishedAsync(application.ApplicationId, channel);

            if (latestPublished is null) {
                throw new NotFoundException(
                    $"Application '{applicationCode}' has no published versions on the '{channel}' channel.");
            }

            var package = await GetPreferredPackageAsync(latestPublished);

            return new UpdateCheckResultDTO {
                UpdateAvailable = true,
                CurrentVersion = string.Empty,
                LatestVersion = latestPublished.VersionString,
                ReleaseType = latestPublished.ReleaseType.ToString(),
                DownloadUrl = package is null ? null : $"/api/packages/{package.UpdatePackageId}/download",
                Checksum = package?.Checksum,
                PackageType = package?.PackageType.ToString(),
                FileSize = package?.FileSize,
                ReleaseNotes = latestPublished.ReleaseNotes,
                Mandatory = latestPublished.IsMandatory
            };
        }

        private async Task<UpdatePackage?> GetPreferredPackageAsync(ApplicationVersion version) {
            var packages = await _packageRepository.GetByApplicationVersionIdAsync(version.ApplicationVersionId);

            return packages.FirstOrDefault(p => p.PackageType == PackageType.Msi)
                ?? packages.FirstOrDefault(p => p.PackageType == PackageType.Exe)
                ?? packages.FirstOrDefault(p => p.PackageType == PackageType.Zip);
        }

        private async Task<Application> GetApplicationOrThrowAsync(string applicationCode) {
            var application = await _applicationRepository.GetByCodeAsync(applicationCode);
            if (application is null) {
                throw new NotFoundException($"Application '{applicationCode}' was not found.");
            }

            return application;
        }
    }
}
