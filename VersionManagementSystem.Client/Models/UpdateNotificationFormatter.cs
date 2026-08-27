using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManagementSystem.Client.Models {
    /// <summary>
    /// Turns an UpdateCheckResult into the plain-text notification blocks the target
    /// application can show (WinForms label text, MessageBox body, console output, etc).
    /// </summary>
    public static class UpdateNotificationFormatter {
        public static string Format(this UpdateCheckResult result, string applicationName) {
            var builder = new StringBuilder();
            var title = result.Mandatory ? "Required Update" : "Update Available";

            builder.AppendLine(new string('-', 50));
            builder.AppendLine(title);
            builder.AppendLine(new string('-', 50));
            builder.AppendLine();
            builder.AppendLine(applicationName);
            builder.AppendLine();

            if (result.Mandatory) {
                builder.AppendLine("Your application version is no longer supported.");
                builder.AppendLine();
                builder.AppendLine($"Current Version: {result.CurrentVersion}");
                builder.AppendLine($"Required Version: {result.LatestVersion}");
                builder.AppendLine();
                builder.AppendLine("Please update before continuing.");
            }
            else {
                builder.AppendLine($"Current Version: {result.CurrentVersion}");
                builder.AppendLine($"Latest Version: {result.LatestVersion}");

                if (!string.IsNullOrWhiteSpace(result.ReleaseNotes)) {
                    builder.AppendLine();
                    builder.AppendLine("What's New:");
                    foreach (var line in result.ReleaseNotes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                        builder.AppendLine($"- {line}");
                    }
                }
            }

            builder.AppendLine(new string('-', 50));
            return builder.ToString();
        }
    }
}
