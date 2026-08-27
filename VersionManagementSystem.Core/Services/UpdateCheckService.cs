using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Core.Models;

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
                await _clientTrackingService.RecordCheckInAsync(applicationCode, machineName, current.ToString());
            }

            var latestPublished = await _versionRepository.GetLatestPublishedAsync(application.ApplicationId, channel);

            if (latestPublished is null) {
                return new UpdateCheckResultDTO {
                    UpdateAvailable = false,
                    CurrentVersion = current.ToString(),
                    LatestVersion = current.ToString(),
                    Mandatory = false
                };
            }

            var latest = new SemanticVersion(latestPublished.Major, latestPublished.Minor, latestPublished.Patch);
            var updateAvailable = current.IsOlderThan(latest);

            var mandatory = latestPublished.IsMandatory;
            if (!mandatory
                && !string.IsNullOrWhiteSpace(latestPublished.MinimumSupportedVersion)
                && _versionService.TryParse(latestPublished.MinimumSupportedVersion, out var minSupported)
                && minSupported is not null) {
                mandatory = current.IsOlderThan(minSupported);
            }

            return new UpdateCheckResultDTO {
                UpdateAvailable = updateAvailable,
                CurrentVersion = current.ToString(),
                LatestVersion = latest.ToString(),
                ReleaseType = latestPublished.ReleaseType.ToString(),
                DownloadUrl = updateAvailable ? await BuildDownloadUrlAsync(latestPublished) : null,
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

            return new UpdateCheckResultDTO {
                UpdateAvailable = true,
                CurrentVersion = string.Empty,
                LatestVersion = latestPublished.VersionString,
                ReleaseType = latestPublished.ReleaseType.ToString(),
                DownloadUrl = await BuildDownloadUrlAsync(latestPublished),
                ReleaseNotes = latestPublished.ReleaseNotes,
                Mandatory = latestPublished.IsMandatory
            };
        }

        /// <summary>Prefers an installer package (Msi/Exe) over a raw Zip when several package types exist.</summary>
        private async Task<string?> BuildDownloadUrlAsync(ApplicationVersion version) {
            var packages = await _packageRepository.GetByApplicationVersionIdAsync(version.ApplicationVersionId);

            var preferred = packages.FirstOrDefault(p => p.PackageType == PackageType.Msi)
                ?? packages.FirstOrDefault(p => p.PackageType == PackageType.Exe)
                ?? packages.FirstOrDefault(p => p.PackageType == PackageType.Zip);

            return preferred is null ? null : $"/api/packages/{preferred.UpdatePackageId}/download";
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
