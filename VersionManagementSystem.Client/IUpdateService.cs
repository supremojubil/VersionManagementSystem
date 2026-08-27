using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client.Models;

namespace VersionManagementSystem.Client {
    public interface IUpdateService {
        Task<UpdateCheckResult> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default);
    }
}
