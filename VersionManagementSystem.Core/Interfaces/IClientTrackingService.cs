using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IClientTrackingService {
        /// <summary>Upserts a ClientInstallation row reflecting the machine's last check-in and reported version.</summary>
        Task RecordCheckInAsync(string applicationCode, string machineName, string currentVersion);

        /// <summary>Records the outcome of an update attempt and refreshes the machine's ClientInstallation row.</summary>
        Task<UpdateHistoryDTO> RecordUpdateHistoryAsync(RecordUpdateHistoryDTO request);
    }
}
