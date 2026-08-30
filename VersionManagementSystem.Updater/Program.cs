using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VersionManagementSystem.Client;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Updater.Services;

namespace VersionManagementSystem.Updater {
    public class Program {
        public static async Task<int> Main(string[] args) {
            Dictionary<string, string> options;
            try {
                options = ParseArgs(args);
                RequireOption(options, "app");
                RequireOption(options, "install-path");
                RequireOption(options, "package");
                RequireOption(options, "package-type");
                RequireOption(options, "from-version");
                RequireOption(options, "to-version");
                RequireOption(options, "main-exe");
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
                PrintUsage();
                return 1;
            }

            var applicationCode = options["app"];
            var installPath = options["install-path"];
            var packagePath = options["package"];
            var fromVersion = options["from-version"];
            var toVersion = options["to-version"];
            var mainExePath = options["main-exe"];

            if (!Version.TryParse(fromVersion, out var parsedFromVersion) || parsedFromVersion is null || parsedFromVersion.Build < 0 || parsedFromVersion.Revision < 0 ||
                !Version.TryParse(toVersion, out var parsedToVersion) || parsedToVersion is null || parsedToVersion.Build < 0 || parsedToVersion.Revision < 0) {
                Console.Error.WriteLine("'--from-version' and '--to-version' must be complete four-part .NET versions such as 2026.8.26.1.");
                return 1;
            }

            if (parsedToVersion.CompareTo(parsedFromVersion) <= 0) {
                Console.Error.WriteLine($"Target version {parsedToVersion.ToString(4)} must be newer than source version {parsedFromVersion.ToString(4)}.");
                return 1;
            }

            if (!File.Exists(mainExePath)) {
                Console.Error.WriteLine($"Main executable was not found: {mainExePath}");
                return 1;
            }

            try {
                var installedAssemblyVersion = AssemblyName.GetAssemblyName(mainExePath).Version;
                if (installedAssemblyVersion is null || installedAssemblyVersion.CompareTo(parsedFromVersion) != 0) {
                    Console.Error.WriteLine($"Installed executable version {installedAssemblyVersion?.ToString(4) ?? "unknown"} does not match --from-version {parsedFromVersion.ToString(4)}.");
                    return 1;
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Could not read AssemblyVersion from '{mainExePath}': {ex.Message}");
                return 1;
            }

            if (!Enum.TryParse<PackageType>(options["package-type"], ignoreCase: true, out var packageType)) {
                Console.Error.WriteLine($"'{options["package-type"]}' is not a valid package type (Zip, Exe, Msi).");
                return 1;
            }

            if (options.TryGetValue("wait-pid", out var waitPidRaw) && int.TryParse(waitPidRaw, out var waitPid)) {
                WaitForProcessExit(waitPid);
            }

            IRollbackService rollbackService = new RollbackService();
            IInstallationService installationService = new InstallationService();

            string status;
            try {
                Console.WriteLine($"Backing up current version {fromVersion}...");
                await rollbackService.CreateBackupAsync(installPath, applicationCode, fromVersion);

                Console.WriteLine($"Installing version {toVersion}...");
                var installed = await installationService.InstallAsync(packagePath, packageType, installPath);

                if (!installed) {
                    throw new InvalidOperationException("The installer reported a non-zero exit code.");
                }

                Console.WriteLine("Verifying installation AssemblyVersion...");
                var verified = await installationService.VerifyInstallationAsync(installPath, mainExePath, parsedToVersion.ToString(4));

                if (!verified) {
                    throw new InvalidOperationException("Post-install verification failed.");
                }

                Console.WriteLine($"Update to {toVersion} completed successfully.");
                status = "Success";
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Update failed: {ex.Message}");
                Console.WriteLine($"Rolling back to {fromVersion}...");

                try {
                    await rollbackService.RollbackAsync(installPath, applicationCode, fromVersion);
                    Console.WriteLine("Rollback completed. The previous version has been restored.");
                    status = "RolledBack";
                }
                catch (Exception rollbackEx) {
                    Console.Error.WriteLine($"Rollback also failed: {rollbackEx.Message}");
                    status = "Failed";
                }
            }

            if (options.TryGetValue("server", out var serverBaseUrl) && !string.IsNullOrWhiteSpace(serverBaseUrl)) {
                await TryReportHistoryAsync(serverBaseUrl, applicationCode, fromVersion, toVersion, status);
            }

            RelaunchMainApplication(mainExePath);

            return status == "Success" ? 0 : 1;
        }

        private static async Task TryReportHistoryAsync(string serverBaseUrl, string applicationCode, string fromVersion, string toVersion, string status) {
            try {
                using var updateService = new ApplicationUpdateService(serverBaseUrl, applicationCode);
                await updateService.ReportUpdateResultAsync(fromVersion, toVersion, status);
            }
            catch (Exception ex) {
                // Reporting is best-effort — a failed report should never block finishing the update.
                Console.Error.WriteLine($"Warning: could not report update history to the server: {ex.Message}");
            }
        }

        private static void RelaunchMainApplication(string mainExePath) {
            try {
                Console.WriteLine("Starting main application...");
                Process.Start(new ProcessStartInfo(mainExePath) { UseShellExecute = true });
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Could not relaunch the main application automatically: {ex.Message}");
            }
        }

        private static void WaitForProcessExit(int processId) {
            try {
                using var process = Process.GetProcessById(processId);
                Console.WriteLine($"Waiting for process {processId} to exit...");
                process.WaitForExit();
            }
            catch (ArgumentException) {
                // Process already exited before the updater started watching it — nothing to wait for.
            }
        }

        private static Dictionary<string, string> ParseArgs(string[] args) {
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++) {
                var token = args[i];
                if (!token.StartsWith("--", StringComparison.Ordinal)) {
                    throw new ArgumentException($"Unexpected argument '{token}'.");
                }

                var key = token[2..];
                if (i + 1 >= args.Length) {
                    throw new ArgumentException($"Missing value for '--{key}'.");
                }

                options[key] = args[++i];
            }

            return options;
        }

        private static void RequireOption(Dictionary<string, string> options, string key) {
            if (!options.ContainsKey(key) || string.IsNullOrWhiteSpace(options[key])) {
                throw new ArgumentException($"'--{key}' is required.");
            }
        }

        private static void PrintUsage() {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: VersionManagementSystem.Updater.exe --app <code> --install-path <path>");
            Console.Error.WriteLine("  --package <file> --package-type <Zip|Exe|Msi> --from-version <v> --to-version <v>");
            Console.Error.WriteLine("  --main-exe <path> [--wait-pid <pid>] [--server <baseUrl>]");
            Console.Error.WriteLine("  Versions use .NET AssemblyVersion format: Major.Minor.Build.Revision (e.g. 2026.8.26.1).");
        }
    }
}
