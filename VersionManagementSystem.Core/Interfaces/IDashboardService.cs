using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IDashboardService {
        Task<DashboardSummaryDTO> GetSummaryAsync();
    }
}
