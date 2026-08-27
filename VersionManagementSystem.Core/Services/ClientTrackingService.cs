using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Entities;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class ClientTrackingService : IClientTrackingService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IClientInstallationRepository _installationRepository;
        private readonly IUpdateHistoryRepository _historyRepository;
        public ClientTrackingService(IApplicationRepository applicationRepository, IClientInstallationRepository installationRepository, IUpdateHistoryRepository historyRepository) {
            _applicationRepository = applicationRepository;
            _installationRepository = installationRepository;
            _historyRepository = historyRepository;
        }

        public async Task RecordCheckInAsync(string applicationCode, string machineName, string currentVersion) {
            var application = await GetApplicationOrThrowAsync(applicationCode);
            var installation = await _installationRepository.GetAsync(application.ApplicationId, machineName);

            if (installation is null) {
                installation = new ClientInstallation {
                    ApplicationId = application.ApplicationId,
                    MachineName = machineName,
                    CurrentVersion = currentVersion,
                    LastChecked = DateTime.UtcNow
                };
                await _installationRepository.AddAsync(installation);
            }
            else {
                installation.CurrentVersion = currentVersion;
                installation.LastChecked = DateTime.UtcNow;
                await _installationRepository.UpdateAsync(installation);
            }

            await _installationRepository.SaveChangesAsync();
        }

        public async Task<UpdateHistoryDTO> RecordUpdateHistoryAsync(RecordUpdateHistoryDTO request) {
            if (string.IsNullOrWhiteSpace(request.MachineName)) {
                throw new ValidationException("Machine name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Status)) {
                throw new ValidationException("Status is required (e.g. Success, Failed, RolledBack).");
            }

            var application = await GetApplicationOrThrowAsync(request.ApplicationCode);

            var history = new UpdateHistory {
                ApplicationId = application.ApplicationId,
                FromVersion = request.FromVersion,
                ToVersion = request.ToVersion,
                UpdateDate = DateTime.UtcNow,
                Status = request.Status.Trim(),
                MachineName = request.MachineName.Trim()
            };

            await _historyRepository.AddAsync(history);
            await _historyRepository.SaveChangesAsync();

            var isSuccessfulUpdate = string.Equals(request.Status.Trim(), "Success", StringComparison.OrdinalIgnoreCase);

            var installation = await _installationRepository.GetAsync(application.ApplicationId, history.MachineName);
            if (installation is null) {
                installation = new ClientInstallation {
                    ApplicationId = application.ApplicationId,
                    MachineName = history.MachineName,
                    CurrentVersion = isSuccessfulUpdate ? request.ToVersion : request.FromVersion,
                    LastChecked = DateTime.UtcNow,
                    LastUpdated = isSuccessfulUpdate ? DateTime.UtcNow : null
                };
                await _installationRepository.AddAsync(installation);
            }
            else {
                installation.LastChecked = DateTime.UtcNow;
                if (isSuccessfulUpdate) {
                    installation.CurrentVersion = request.ToVersion;
                    installation.LastUpdated = DateTime.UtcNow;
                }
                await _installationRepository.UpdateAsync(installation);
            }

            await _installationRepository.SaveChangesAsync();

            return new UpdateHistoryDTO {
                UpdateHistoryId = history.UpdateHistoryId,
                ApplicationCode = application.ApplicationCode,
                FromVersion = history.FromVersion,
                ToVersion = history.ToVersion,
                UpdateDate = history.UpdateDate,
                Status = history.Status,
                MachineName = history.MachineName
            };
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
