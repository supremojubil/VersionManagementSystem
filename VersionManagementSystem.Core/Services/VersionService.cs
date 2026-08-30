using System;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.Core.Services {
    /// <summary>Validates and compares .NET assembly-style versions (Major.Minor.Build.Revision).</summary>
    public sealed class VersionService : IVersionService {
        public Version Parse(string version) {
            if (!TryParse(version, out var parsed) || parsed is null) {
                throw new ValidationException($"'{version}' is not a valid .NET assembly version. Expected Major.Minor.Build.Revision (for example 2026.8.26.1).");
            }

            return parsed;
        }

        public bool TryParse(string version, out Version? parsed) {
            parsed = null;
            if (string.IsNullOrWhiteSpace(version)) {
                return false;
            }

            if (!System.Version.TryParse(version.Trim(), out var candidate) || candidate is null) {
                return false;
            }

            // AssemblyVersion supports up to four numeric components. Reject negative/oversized values
            // through Version's parser and reject more than four components by requiring the canonical form.
            if (candidate.Major < 0 || candidate.Minor < 0 || candidate.Build < 0 || candidate.Revision < 0) {
                return false;
            }

            parsed = candidate;
            return true;
        }

        public int Compare(string left, string right) => Parse(left).CompareTo(Parse(right));

        public bool IsNewer(string candidate, string baseline) => Parse(candidate).CompareTo(Parse(baseline)) > 0;

        public bool IsOlder(string candidate, string baseline) => Parse(candidate).CompareTo(Parse(baseline)) < 0;

        public bool IsEqual(string left, string right) => Parse(left).CompareTo(Parse(right)) == 0;
    }
}
