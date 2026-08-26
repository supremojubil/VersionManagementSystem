using System;
using System.Text.RegularExpressions;

namespace VersionManagementSystem.Core.Models {
    /// <summary>
    /// Reusable semantic version value object (MAJOR.MINOR.PATCH).
    /// Never compare versions as plain strings — always use this type.
    /// </summary>
    public sealed class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion> {
        private static readonly Regex VersionPattern = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$", RegexOptions.Compiled);

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        public SemanticVersion(int major, int minor, int patch) {
            if (major < 0)
                throw new ArgumentOutOfRangeException(nameof(major), "Major cannot be negative.");
            if (minor < 0)
                throw new ArgumentOutOfRangeException(nameof(minor), "Minor cannot be negative.");
            if (patch < 0)
                throw new ArgumentOutOfRangeException(nameof(patch), "Patch cannot be negative.");

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Parses a "MAJOR.MINOR.PATCH" string. Throws FormatException on invalid input.
        /// </summary>
        public static SemanticVersion Parse(string value) {
            if (!TryParse(value, out var version)) {
                throw new FormatException($"'{value}' is not a valid semantic version (expected MAJOR.MINOR.PATCH).");
            }

            return version!;
        }

        /// <summary>
        /// Attempts to parse a "MAJOR.MINOR.PATCH" string without throwing.
        /// </summary>
        public static bool TryParse(string? value, out SemanticVersion? version) {
            version = null;

            if (string.IsNullOrWhiteSpace(value)) {
                return false;
            }

            var match = VersionPattern.Match(value.Trim());
            if (!match.Success) {
                return false;
            }

            if (!int.TryParse(match.Groups[1].Value, out var major) ||
                !int.TryParse(match.Groups[2].Value, out var minor) ||
                !int.TryParse(match.Groups[3].Value, out var patch)) {
                return false;
            }

            version = new SemanticVersion(major, minor, patch);
            return true;
        }

        /// <summary>
        /// Compares this version to another. Returns &lt;0 if older, 0 if equal, &gt;0 if newer.
        /// </summary>
        public int Compare(SemanticVersion other) {
            return CompareTo(other);
        }

        public int CompareTo(SemanticVersion? other) {
            if (other is null)
                return 1;

            var majorCompare = Major.CompareTo(other.Major);
            if (majorCompare != 0)
                return majorCompare;

            var minorCompare = Minor.CompareTo(other.Minor);
            if (minorCompare != 0)
                return minorCompare;

            return Patch.CompareTo(other.Patch);
        }

        public bool IsNewerThan(SemanticVersion other) => CompareTo(other) > 0;

        public bool IsOlderThan(SemanticVersion other) => CompareTo(other) < 0;

        public bool IsEqualTo(SemanticVersion other) => CompareTo(other) == 0;

        public bool Equals(SemanticVersion? other) => other is not null && IsEqualTo(other);

        public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        public static bool operator ==(SemanticVersion? left, SemanticVersion? right) {
            if (left is null)
                return right is null;
            return left.Equals(right);
        }

        public static bool operator != (SemanticVersion? left, SemanticVersion? right) => !(left == right);

        public static bool operator > (SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;

        public static bool operator < (SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;

        public static bool operator >= (SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

        public static bool operator <= (SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;
    }
}
