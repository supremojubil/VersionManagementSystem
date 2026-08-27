using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    public sealed class DashboardService : IDashboardService {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IApplicationVersionRepository _versionRepository;
        private readonly IUpdateHistoryRepository _historyRepository;
        public DashboardService(IApplicationRepository applicationRepository, IApplicationVersionRepository versionRepository, IUpdateHistoryRepository historyRepository) {
            _applicationRepository = applicationRepository;
            _versionRepository = versionRepository;
            _historyRepository = historyRepository;
        }

        public async Task<DashboardSummaryDTO> GetSummaryAsync() {
            var applications = await _applicationRepository.GetAllAsync(includeInactive: true);
            var publishedCount = await _versionRepository.CountPublishedAsync();
            var pendingCount = await _versionRepository.CountPendingAsync();

            var startOfTodayUtc = DateTime.UtcNow.Date;
            var updatesToday = await _historyRepository.CountSinceAsync(startOfTodayUtc);

            return new DashboardSummaryDTO {
                Applications = applications.Count,
                PublishedVersions = publishedCount,
                PendingReleases = pendingCount,
                UpdatesToday = updatesToday
            };
        }
    }
}
