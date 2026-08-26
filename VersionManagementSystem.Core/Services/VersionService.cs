using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;
using VersionManagementSystem.Core.Models;

namespace VersionManagementSystem.Core.Services {
    public sealed class VersionService : IVersionService {
        public SemanticVersion Parse(string version) {
            if (!SemanticVersion.TryParse(version, out var parsed) || parsed is null) {
                throw new ValidationException($"'{version}' is not a valid version. Expected format: MAJOR.MINOR.PATCH (e.g. 1.5.0).");
            }

            return parsed;
        }

        public bool TryParse(string version, out SemanticVersion? parsed) {
            return SemanticVersion.TryParse(version, out parsed);
        }

        public int Compare(string left, string right) {
            return Parse(left).CompareTo(Parse(right));
        }

        public bool IsNewer(string candidate, string baseline) {
            return Parse(candidate).IsNewerThan(Parse(baseline));
        }

        public bool IsOlder(string candidate, string baseline) {
            return Parse(candidate).IsOlderThan(Parse(baseline));
        }

        public bool IsEqual(string left, string right) {
            return Parse(left).IsEqualTo(Parse(right));
        }
    }
}
