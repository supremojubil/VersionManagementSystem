using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VersionManagementSystem.Core.Models;

namespace VersionManagementSystem.PublishTool {
    /// <summary>
    /// VersionManagementSystem.PublishTool.exe
    ///
    /// Turns "register a version, upload a package, walk it through the workflow" — several
    /// manual Swagger calls — into one command. The version number is never typed by hand:
    /// it's read either from the compiled exe/dll's file-version metadata (which is exactly
    /// what AssemblyInfo.cs's [assembly: AssemblyFileVersion(...)] produces at build time), or,
    /// if you'd rather not build first, parsed directly out of an AssemblyInfo.cs source file.
    ///
    /// Examples:
    ///
    ///   Read the version from the built exe, zip the whole Release folder, upload and publish:
    ///     VersionManagementSystem.PublishTool.exe
    ///       --app FJD
    ///       --bin-path "C:\Projects\FelysJewelryDesktop\bin\Release\net8.0-windows"
    ///       --main-exe FelysJewelryDesktop.exe
    ///       --server https://localhost:5001/api
    ///       --release-notes "Bug fixes and improvements"
    ///       --publish
    ///
    ///   Read the version from AssemblyInfo.cs instead of a built binary:
    ///     VersionManagementSystem.PublishTool.exe
    ///       --app FJD
    ///       --bin-path "C:\Projects\FelysJewelryDesktop\bin\Release\net8.0-windows"
    ///       --assembly-info "C:\Projects\FelysJewelryDesktop\Properties\AssemblyInfo.cs"
    ///       --server https://localhost:5001/api
    /// </summary>
    public static class Program {
        private static readonly Regex AssemblyFileVersionPattern = new(@"\[assembly:\s*AssemblyFileVersion\(""([^""]+)""\)\]", RegexOptions.Compiled);

        private static readonly Regex AssemblyVersionPattern = new(@"\[assembly:\s*AssemblyVersion\(""([^""]+)""\)\]", RegexOptions.Compiled);

        public static async Task<int> Main(string[] args) {
            Dictionary<string, string> options;
            HashSet<string> flags;
            try {
                (options, flags) = ParseArgs(args, booleanFlags: new[] { "publish", "mandatory" });
                Require(options, "app");
                Require(options, "bin-path");
                Require(options, "server");

                if (!options.ContainsKey("main-exe") && !options.ContainsKey("assembly-info")) {
                    throw new ArgumentException("Provide either --main-exe (reads the built binary's version) " + "or --assembly-info (reads AssemblyInfo.cs directly).");
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

            SemanticVersion version;
            try {
                version = options.ContainsKey("main-exe") ? ReadVersionFromBinary(Path.Combine(binPath, options["main-exe"])) : ReadVersionFromAssemblyInfo(options["assembly-info"]);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Could not determine version: {ex.Message}");
                return 1;
            }

            Console.WriteLine($"Detected version: {version}");

            var zipPath = Path.Combine(Path.GetTempPath(), $"{applicationCode}_{version}.zip");
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

        /// <summary>
        /// Reads the version straight from the built binary's file-version metadata — this is
        /// exactly what [assembly: AssemblyFileVersion("1.5.0.0")] in AssemblyInfo.cs compiles
        /// into, so it's guaranteed to match what actually shipped in this release folder.
        /// </summary>
        private static SemanticVersion ReadVersionFromBinary(string exePath) {
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException($"Main executable not found at '{exePath}'.");
            }

            var info = FileVersionInfo.GetVersionInfo(exePath);

            // AssemblyVersion/AssemblyFileVersion is Major.Minor.Build.Revision — the update
            // system only tracks Major.Minor.Patch, so Build maps to Patch and Revision is dropped.
            return new SemanticVersion(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart);
        }

        /// <summary>
        /// Parses AssemblyInfo.cs directly, for when you want to publish without building first.
        /// Prefers AssemblyFileVersion; falls back to AssemblyVersion if that attribute isn't present.
        /// </summary>
        private static SemanticVersion ReadVersionFromAssemblyInfo(string assemblyInfoPath) {
            if (!File.Exists(assemblyInfoPath)) {
                throw new FileNotFoundException($"AssemblyInfo.cs not found at '{assemblyInfoPath}'.");
            }

            var content = File.ReadAllText(assemblyInfoPath);

            var match = AssemblyFileVersionPattern.Match(content);
            if (!match.Success) {
                match = AssemblyVersionPattern.Match(content);
            }

            if (!match.Success) {
                throw new InvalidOperationException("No [assembly: AssemblyFileVersion(...)] or [assembly: AssemblyVersion(...)] attribute found.");
            }

            var rawVersion = match.Groups[1].Value; // e.g. "1.5.0.0" or "1.5.*"
            var parts = rawVersion.Split('.');

            if (parts.Length < 2 || !int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor)) {
                throw new InvalidOperationException($"'{rawVersion}' could not be parsed into Major.Minor.Patch.");
            }

            var patch = parts.Length >= 3 && int.TryParse(parts[2], out var parsedPatch) ? parsedPatch : 0;
            return new SemanticVersion(major, minor, patch);
        }

        private static async Task RegisterVersionAsync(HttpClient httpClient, string applicationCode, SemanticVersion version, Dictionary<string, string> options, HashSet<string> flags) {
            var payload = new {
                applicationCode,
                version = version.ToString(),
                releaseType = options.GetValueOrDefault("release-type", "Minor"),
                releaseDate = DateTime.UtcNow,
                releaseNotes = options.GetValueOrDefault("release-notes"),
                minimumSupportedVersion = options.GetValueOrDefault("min-version"),
                isMandatory = flags.Contains("mandatory"),
                channel = options.GetValueOrDefault("channel", "Stable"),
                createdBy = options.GetValueOrDefault("created-by", Environment.UserName)
            };

            Console.WriteLine($"Registering version {version}...");
            using var response = await httpClient.PostAsJsonAsync($"applications/{applicationCode}/versions", payload);

            if (response.IsSuccessStatusCode) {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();

            // The version may already exist from a previous run (e.g. re-uploading a package
            // after a build failure) — that's fine, we just move on to the package upload.
            if (body.Contains("already exists", StringComparison.OrdinalIgnoreCase)) {
                Console.WriteLine($"Version {version} is already registered — continuing.");
                return;
            }

            throw new InvalidOperationException($"Failed to register version ({(int)response.StatusCode}): {body}");
        }

        private static async Task UploadPackageAsync(HttpClient httpClient, string applicationCode, SemanticVersion version, string zipPath) {
            Console.WriteLine("Uploading package...");

            using var content = new MultipartFormDataContent();
            await using var fileStream = File.OpenRead(zipPath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
            content.Add(streamContent, "file", Path.GetFileName(zipPath));

            using var response = await httpClient.PostAsync($"applications/{applicationCode}/versions/{version}/package", content);

            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();

                if (body.Contains("already exists", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine("A package for this version/type already exists — skipping upload.");
                    return;
                }

                throw new InvalidOperationException($"Failed to upload package ({(int)response.StatusCode}): {body}");
            }
        }

        private static async Task AdvanceThroughWorkflowAsync(HttpClient httpClient, string applicationCode, SemanticVersion version) {
            var basePath = $"applications/{applicationCode}/versions/{version}";

            await PostWorkflowStepAsync(httpClient, $"{basePath}/submit-for-testing", "Submit for testing");
            await PostWorkflowStepAsync(httpClient, $"{basePath}/approve", "Approve");
            await PostWorkflowStepAsync(httpClient, $"{basePath}/publish", "Publish");
        }

        private static async Task PostWorkflowStepAsync(HttpClient httpClient, string relativeUrl, string stepName) {
            Console.WriteLine($"{stepName}...");
            using var response = await httpClient.PostAsync(relativeUrl, content: null);

            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync();

                // Already past this step from a previous run (e.g. re-running --publish) — not fatal.
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
            Console.Error.WriteLine("  [--release-type Minor] [--release-notes \"...\"] [--min-version 1.4.0]");
            Console.Error.WriteLine("  [--channel Stable] [--mandatory] [--created-by name] [--publish]");
        }
    }
}
