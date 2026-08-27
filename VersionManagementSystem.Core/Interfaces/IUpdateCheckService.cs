using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Enums;

namespace VersionManagementSystem.Core.Interfaces {
    public interface IUpdateCheckService {
        // <summary>
        /// Compares a client's current version against the latest Published version on the given
        /// channel. Optionally records the check-in for a machine (Phase 7 client tracking).
        /// </summary>
       Task<UpdateCheckResultDTO> CheckForUpdateAsync(string applicationCode, string currentVersion, UpdateChannel channel, string? machineName);

        /// <summary>Returns the latest Published version on the given channel, with no current-version comparison.</summary>
        Task<UpdateCheckResultDTO> GetLatestAsync(string applicationCode, UpdateChannel channel);
    }
}
