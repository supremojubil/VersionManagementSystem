using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class ApplicationVersionService : IApplicationVersionService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;
        private readonly IVersionService _versionService;
        private readonly IReleaseNoteRepository _releaseNoteRepository;
        private readonly IUpdatePackageRepository _packageRepository;

        public ApplicationVersionService(
            IApplicationRepository applicationRepository,
            IApplicationVersionRepository versionRepository,
            IVersionService versionService,
            IReleaseNoteRepository releaseNoteRepository,
            IUpdatePackageRepository packageRepository) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
            _versionService = versionService;
            _releaseNoteRepository = releaseNoteRepository;
            _packageRepository = packageRepository;
        }

        public async Task<IReadOnlyList<ApplicationVersionDTO>> GetHistoryAsync(string applicationCode) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var history = await _versionRepository.GetHistoryAsync(application.ApplicationId);

            var ordered = history
                .OrderByDescending(c => c.Major)
                .ThenByDescending(c => c.Minor)
                .ThenByDescending(c => c.Patch)
                .ToList();

            var result = new List<ApplicationVersionDTO>(ordered.Count);
            foreach (var version in ordered) {
                result.Add(await MapToDtoAsync(version, application.ApplicationCode));
            }

            return result;
        }

        public async Task<ApplicationVersionDTO> GetLatestAsync(string applicationCode) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var latest = await _versionRepository.GetLatestAsync(application.ApplicationId);

            if (latest == null) {
                throw new NotFoundException($"Application '{applicationCode}' has no registered versions yet.");
            }

            return await MapToDtoAsync(latest, application.ApplicationCode);
        }

        public async Task<ApplicationVersionDTO> CreateAsync(CreateApplicationVersionDTO request) {
            var application = await GetApplicationOrThrowAsync(request.ApplicationCode);

            var semanticVersion = _versionService.Parse(request.Version);

            if (!Enum.TryParse<ReleaseType>(request.ReleaseType, ignoreCase: true, out var releaseType)) {
                throw new ValidationException($"'{request.ReleaseType}' is not a valid release type. Expected Major, Minor, Patch or Hotfix.");
            }

            if (await _versionRepository.VersionExistsAsync(application.ApplicationId, semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch)) {
                throw new ValidationException($"Version {semanticVersion} already exists for application '{application.ApplicationCode}'.");
            }

            var latest = await _versionRepository.GetLatestAsync(application.ApplicationId);
            if (latest != null) {
                var latestVersion = new Models.SemanticVersion(latest.Major, latest.Minor, latest.Patch);
                if (!semanticVersion.IsNewerThan(latestVersion)) {
                    throw new ValidationException($"New version {semanticVersion} must be newer than the current latest version {latestVersion}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.MinimumSupportedVersion) &&
                !_versionService.TryParse(request.MinimumSupportedVersion, out _)) {
                throw new ValidationException($"'{request.MinimumSupportedVersion}' is not a valid minimum supported version.");
            }

            if (!Enum.TryParse<UpdateChannel>(request.Channel, ignoreCase: true, out var channel)) {
                throw new ValidationException($"'{request.Channel}' is not a valid channel. Expected Stable, Beta or Development.");
            }

            var version = new ApplicationVersion {
                ApplicationId = application.ApplicationId,
                Major = semanticVersion.Major,
                Minor = semanticVersion.Minor,
                Patch = semanticVersion.Patch,
                ReleaseType = releaseType,
                ReleaseStatus = ReleaseStatus.Draft,
                ReleaseDate = request.ReleaseDate,
                ReleaseNotes = request.ReleaseNotes,
                MinimumSupportedVersion = request.MinimumSupportedVersion,
                IsMandatory = request.IsMandatory,
                Channel = channel,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            await _versionRepository.AddAsync(version);
            await _versionRepository.SaveChangesAsync();

            return await MapToDtoAsync(version, application.ApplicationCode);
        }

        private async Task<Application> GetApplicationOrThrowAsync(string applicationCode) {
            var application = await _applicationRepository.GetByCodeAsync(applicationCode);
            if (application == null) {
                throw new NotFoundException($"Application '{applicationCode}' was not found.");
            }

            return application;
        }

        private async Task<ApplicationVersionDTO> MapToDtoAsync(ApplicationVersion version, string applicationCode) {
            var notes = await _releaseNoteRepository.GetByApplicationVersionIdAsync(version.ApplicationVersionId);
            var packages = await _packageRepository.GetByApplicationVersionIdAsync(version.ApplicationVersionId);

            return new ApplicationVersionDTO {
                ApplicationVersionId = version.ApplicationVersionId,
                ApplicationId = version.ApplicationId,
                ApplicationCode = applicationCode,
                Version = version.VersionString,
                ReleaseType = version.ReleaseType.ToString(),
                ReleaseStatus = version.ReleaseStatus.ToString(),
                ReleaseDate = version.ReleaseDate,
                ReleaseNotes = version.ReleaseNotes,
                MinimumSupportedVersion = version.MinimumSupportedVersion,
                IsMandatory = version.IsMandatory,
                Channel = version.Channel.ToString(),
                CreatedDate = version.CreatedDate,
                CreatedBy = version.CreatedBy,
                PublishedDate = version.PublishedDate,
                PublishedBy = version.PublishedBy,
                StructuredReleaseNotes = notes
                    .OrderBy(n => n.Category).ThenBy(n => n.SortOrder)
                    .Select(n => new ReleaseNoteDTO {
                        ReleaseNoteId = n.ReleaseNoteId,
                        Category = n.Category.ToString(),
                        Description = n.Description,
                        SortOrder = n.SortOrder
                    }).ToList(),
                Packages = packages.Select(p => new UpdatePackageDTO {
                    UpdatePackageId = p.UpdatePackageId,
                    ApplicationVersionId = p.ApplicationVersionId,
                    ApplicationCode = applicationCode,
                    Version = version.VersionString,
                    FileName = p.FileName,
                    FileSize = p.FileSize,
                    Checksum = p.Checksum,
                    PackageType = p.PackageType.ToString(),
                    CreatedDate = p.CreatedDate,
                    UploadedBy = p.UploadedBy
                }).ToList()
            };
        }
    }
}
