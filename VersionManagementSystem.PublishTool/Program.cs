using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VersionManagementSystem.PublishTool {
    /// <summary>
    /// Publishes an application package using the version embedded in the compiled application.
    /// The application AssemblyVersion is the single source of truth for the published package version.
    /// </summary>
    public static class Program {
        private static readonly Regex AssemblyVersionPattern = new(@"\[assembly:\s*AssemblyVersion\(""([^""]+)""\)\]", RegexOptions.Compiled);

        public static async Task<int> Main(string[] args) {
            Dictionary<string, string> options;
            HashSet<string> flags;

            try {
                (options, flags) = ParseArgs(args, new[] { "publish", "mandatory" });
                Require(options, "app");
                Require(options, "bin-path");
                Require(options, "server");

                if (!options.ContainsKey("main-exe") && !options.ContainsKey("assembly-info")) {
                    throw new ArgumentException("Provide either --main-exe (reads AssemblyVersion from the compiled executable) or --assembly-info (reads AssemblyVersion.cs directly).");
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Invalid arguments: {ex.Message}");
                PrintUsage();
                return 1;
            }

            var applicationCode = options["app"];
            var binPath = options["bin-path"];
            var serverBaseUrl = options["server"].TrimEnd('/') + "/";

            if (!Directory.Exists(binPath)) {
                Console.Error.WriteLine($"Bin path '{binPath}' does not exist.");
                return 1;
            }

            Version version;
            try {
                version = options.ContainsKey("main-exe")
                    ? ReadVersionFromBinary(Path.Combine(binPath, options["main-exe"]))
                    : ReadVersionFromAssemblyInfo(options["assembly-info"]);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Could not determine version: {ex.Message}");
                return 1;
            }

            var versionString = version.ToString(4);
            Console.WriteLine($"Detected AssemblyVersion: {versionString}");

            var zipPath = Path.Combine(Path.GetTempPath(), $"{applicationCode}_{versionString}.zip");
            if (File.Exists(zipPath)) {
                File.Delete(zipPath);
            }

            Console.WriteLine($"Zipping '{binPath}' -> {zipPath}...");
            ZipFile.CreateFromDirectory(binPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            using var httpClient = new HttpClient { BaseAddress = new Uri(serverBaseUrl) };

            try {
                await RegisterVersionAsync(httpClient, applicationCode, version, options, flags);
                await UploadPackageAsync(httpClient, applicationCode, version, zipPath);

                if (flags.Contains("publish")) {
                    await AdvanceThroughWorkflowAsync(httpClient, applicationCode, version);
                }

                Console.WriteLine("Done.");
                return 0;
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Publish failed: {ex.Message}");
                return 1;
            }
            finally {
                File.Delete(zipPath);
            }
        }

        private static Version ReadVersionFromBinary(string exePath) {
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException($"Main executable not found at '{exePath}'.");
            }

            // AssemblyName reads the AssemblyVersion embedded in the compiled assembly.
            // This is the value produced by [assembly: AssemblyVersion("...")].
            var assemblyName = AssemblyName.GetAssemblyName(exePath);
            var version = assemblyName.Version;

            if (version is null || version.Build < 0 || version.Revision < 0) {
                throw new InvalidOperationException(
                    $"The executable '{exePath}' does not contain a complete four-part AssemblyVersion. " +
                    "Expected Major.Minor.Build.Revision, for example 2026.8.26.1.");
            }

            return version;
        }

        private static Version ReadVersionFromAssemblyInfo(string assemblyInfoPath) {
            if (!File.Exists(assemblyInfoPath)) {
                throw new FileNotFoundException($"AssemblyInfo.cs not found at '{assemblyInfoPath}'.");
            }

            var content = File.ReadAllText(assemblyInfoPath);
            var match = AssemblyVersionPattern.Match(content);

            if (!match.Success) {
                throw new InvalidOperationException(
                    "No [assembly: AssemblyVersion(\"...\")] attribute was found in AssemblyInfo.cs.");
            }

            var rawVersion = match.Groups[1].Value.Trim();
            if (!Version.TryParse(rawVersion, out var version) || version is null || version.Build < 0 || version.Revision < 0) {
                throw new InvalidOperationException(
                    $"'{rawVersion}' is not a complete four-part .NET AssemblyVersion. " +
                    "Expected Major.Minor.Build.Revision, for example 2026.8.26.1.");
            }

            return version;
        }

        private static async Task RegisterVersionAsync(HttpClient httpClient, string applicationCode, Version version, Dictionary<string, string> options, HashSet<string> flags) {
            var versionString = version.ToString(4);
            var payload = new {
                applicationCode,
                version = versionString,
                releaseType = options.GetValueOrDefault("release-type", "Minor"),
                releaseDate = DateTime.UtcNow,
                releaseNotes = options.GetValueOrDefault("release-notes"),
                minimumSupportedVersion = options.GetValueOrDefault("min-version"),
                isMandatory = flags.Contains("mandatory"),
                channel = options.GetValueOrDefault("channel", "Stable"),
                createdBy = options.GetValueOrDefault("created-by", Environment.UserName)
            };

            Console.WriteLine($"Registering version {versionString}...");
            using var response = await httpClient.PostAsJsonAsync($"applications/{applicationCode}/versions", payload);

            if (response.IsSuccessStatusCode) {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            if (body.Contains("already exists", StringComparison.OrdinalIgnoreCase)) {
                Console.WriteLine($"Version {versionString} is already registered — continuing.");
                return;
            }

            throw new InvalidOperationException($"Failed to register version ({(int)response.StatusCode}): {body}");
        }

        private static async Task UploadPackageAsync(HttpClient httpClient, string applicationCode, Version version, string zipPath) {
            var versionString = version.ToString(4);
            Console.WriteLine("Uploading package...");

            using var content = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(zipPath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", Path.GetFileName(zipPath));

            using var response = await httpClient.PostAsync($"applications/{applicationCode}/versions/{versionString}/package", content);

            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();
                if (body.Contains("already exists", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine("A package for this version/type already exists — skipping upload.");
                    return;
                }

                throw new InvalidOperationException($"Failed to upload package ({(int)response.StatusCode}): {body}");
            }
        }

        private static async Task AdvanceThroughWorkflowAsync(HttpClient httpClient, string applicationCode, Version version) {
            var versionString = version.ToString(4);
            var basePath = $"applications/{applicationCode}/versions/{versionString}";

            await PostWorkflowStepAsync(httpClient, $"{basePath}/submit-for-testing", "Submit for testing");
            await PostWorkflowStepAsync(httpClient, $"{basePath}/approve", "Approve");
            await PostWorkflowStepAsync(httpClient, $"{basePath}/publish", "Publish");
        }

        private static async Task PostWorkflowStepAsync(HttpClient httpClient, string relativeUrl, string stepName) {
            Console.WriteLine($"{stepName}...");
            using var response = await httpClient.PostAsync(relativeUrl, content: null);

            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();
                if (body.Contains("currently", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine($"{stepName} skipped: {body}");
                    return;
                }

                throw new InvalidOperationException($"{stepName} failed ({(int)response.StatusCode}): {body}");
            }
        }

        private static (Dictionary<string, string> Options, HashSet<string> Flags) ParseArgs(string[] args, IReadOnlyCollection<string> booleanFlags) {
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++) {
                var token = args[i];
                if (!token.StartsWith("--", StringComparison.Ordinal)) {
                    throw new ArgumentException($"Unexpected argument '{token}'.");
                }

                var key = token[2..];
                if (Contains(booleanFlags, key)) {
                    flags.Add(key);
                    continue;
                }

                if (i + 1 >= args.Length) {
                    throw new ArgumentException($"Missing value for '--{key}'.");
                }

                options[key] = args[++i];
            }

            return (options, flags);
        }

        private static bool Contains(IReadOnlyCollection<string> values, string value) {
            foreach (var item in values) {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        private static void Require(Dictionary<string, string> options, string key) {
            if (!options.ContainsKey(key) || string.IsNullOrWhiteSpace(options[key])) {
                throw new ArgumentException($"'--{key}' is required.");
            }
        }

        private static void PrintUsage() {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Usage: VersionManagementSystem.PublishTool.exe --app <code> --bin-path <folder>");
            Console.Error.WriteLine("  (--main-exe <exeFileName> | --assembly-info <path\\to\\AssemblyInfo.cs>)");
            Console.Error.WriteLine("  --server <apiBaseUrl>");
            Console.Error.WriteLine("  [--release-type Minor] [--release-notes \"...\"] [--min-version 2026.8.26.1]");
            Console.Error.WriteLine("  [--channel Stable] [--mandatory] [--created-by name] [--publish]");
        }
    }
}
