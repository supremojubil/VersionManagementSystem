using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VersionManagementSystem.Client.Exceptions;
using VersionManagementSystem.Client.Models;
using VersionManagementSystem.Core.Services;

namespace VersionManagementSystem.Client {
    /// <summary>
    /// Reusable update-check client for a single target application. Usage:
    ///
    ///   var updater = new ApplicationUpdateService("https://version-server/api", "FJ");
    ///   var result = await updater.CheckForUpdateAsync("2026.8.26.1");
    ///   if (result.UpdateAvailable) { ... }
    ///
    /// The target application never needs to know how versions are stored server-side —
    /// only its own application code and current version.
    /// </summary>
    public sealed class ApplicationUpdateService : IUpdateService, IDisposable {
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;
        private readonly string _applicationCode;
        private readonly string _channel;
        private readonly VersionService _versionService = new();

        public ApplicationUpdateService(string baseUrl, string applicationCode, string channel = "Stable", HttpClient? httpClient = null) {
            if (string.IsNullOrWhiteSpace(baseUrl)) {
                throw new ArgumentException("Base URL is required.", nameof(baseUrl));
            }

            if (string.IsNullOrWhiteSpace(applicationCode)) {
                throw new ArgumentException("Application code is required.", nameof(applicationCode));
            }

            _applicationCode = applicationCode;
            _channel = channel;

            if (httpClient is not null) {
                _httpClient = httpClient;
                _ownsHttpClient = false;
            }
            else {
                _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
                _ownsHttpClient = true;
            }
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(string currentVersion, CancellationToken cancellationToken = default) {
            // Fail fast locally on a malformed version before making a network call.
            _versionService.Parse(currentVersion);

            var requestUri =
                $"update/check?application={Uri.EscapeDataString(_applicationCode)}" +
                $"&version={Uri.EscapeDataString(currentVersion)}" +
                $"&channel={Uri.EscapeDataString(_channel)}" +
                $"&machineName={Uri.EscapeDataString(Environment.MachineName)}";

            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new UpdateCheckException($"Update check failed with status {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<UpdateCheckResult>(JsonOptions, cancellationToken);
            return result ?? throw new UpdateCheckException("The server returned an empty update check response.");
        }

        /// <summary>Reports the outcome of an update attempt back to the server (Phase 7 update history).</summary>
        public async Task ReportUpdateResultAsync(string fromVersion, string toVersion, string status, CancellationToken cancellationToken = default) {
            var payload = new {
                applicationCode = _applicationCode,
                machineName = Environment.MachineName,
                fromVersion,
                toVersion,
                status
            };

            using var response = await _httpClient.PostAsJsonAsync("update/history", payload, cancellationToken);
            if (!response.IsSuccessStatusCode) {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new UpdateCheckException($"Reporting update history failed with status {(int)response.StatusCode}: {body}");
            }
        }

        public void Dispose() {
            if (_ownsHttpClient) {
                _httpClient.Dispose();
            }
        }
    }
}
