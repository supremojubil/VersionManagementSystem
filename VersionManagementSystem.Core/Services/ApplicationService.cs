using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class ApplicationService : IApplicationService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;

        public ApplicationService(IApplicationRepository applicationRepository, IApplicationVersionRepository versionRepository) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
        }

        public async Task<IReadOnlyList<ApplicationDTO>> GetAllAsync(bool includeInactive = false) {
            var applications = await _applicationRepository.GetAllAsync(includeInactive);

            var result = new List<ApplicationDTO>(applications.Count);
            foreach (var application in applications) {
                var latest = await _versionRepository.GetLatestAsync(application.ApplicationId);
                result.Add(MapToDto(application, latest?.VersionString));
            }

            return result;
        }

        public async Task<ApplicationDTO> GetByCodeAsync(string applicationCode) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var latest = await _versionRepository.GetLatestAsync(application.ApplicationId);
            return MapToDto(application, latest?.VersionString);
        }

        public async Task<ApplicationDTO> CreateAsync(CreateApplicationDTO request) {
            ValidateCode(request.ApplicationCode);

            if (string.IsNullOrWhiteSpace(request.ApplicationName)) {
                throw new ValidationException("Application name is required.");
            }

            if (await _applicationRepository.CodeExistsAsync(request.ApplicationCode)) {
                throw new ValidationException($"Application code '{request.ApplicationCode}' is already registered.");
            }

            var application = new Application {
                ApplicationCode = request.ApplicationCode.Trim().ToUpperInvariant(),
                ApplicationName = request.ApplicationName.Trim(),
                Description = request.Description?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _applicationRepository.AddAsync(application);
            await _applicationRepository.SaveChangesAsync();

            return MapToDto(application, currentVersion: null);
        }

        public async Task<ApplicationDTO> UpdateAsync(string applicationCode, UpdateApplicationDTO request) {
            var application = await GetApplicationOrThrowAsync(applicationCode);

            if (string.IsNullOrWhiteSpace(request.ApplicationName)) {
                throw new ValidationException("Application name is required.");
            }

            application.ApplicationName = request.ApplicationName.Trim();
            application.Description = request.Description?.Trim();
            application.IsActive = request.IsActive;

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();

            var latest = await _versionRepository.GetLatestAsync(application.ApplicationId);
            return MapToDto(application, latest?.VersionString);
        }

        public async Task DisableAsync(string applicationCode) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            application.IsActive = false;

            await _applicationRepository.UpdateAsync(application);
            await _applicationRepository.SaveChangesAsync();
        }

        private async Task<Application> GetApplicationOrThrowAsync(string applicationCode) {
            var application = await _applicationRepository.GetByCodeAsync(applicationCode);
            if (application is null) {
                throw new NotFoundException($"Application '{applicationCode}' was not found.");
            }

            return application;
        }

        private static void ValidateCode(string applicationCode) {
            if (string.IsNullOrWhiteSpace(applicationCode)) {
                throw new ValidationException("Application code is required.");
            }

            if (applicationCode.Trim().Length > 20) {
                throw new ValidationException("Application code must be 20 characters or fewer.");
            }
        }

        private static ApplicationDTO MapToDto(Application application, string? currentVersion) {
            return new ApplicationDTO {
                ApplicationId = application.ApplicationId,
                ApplicationCode = application.ApplicationCode,
                ApplicationName = application.ApplicationName,
                Description = application.Description,
                IsActive = application.IsActive,
                CurrentVersion = currentVersion,
                CreatedDate = application.CreatedDate
            };
        }
    }
}
