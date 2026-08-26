using VersionManagementSystem.Core.Models;

namespace VersionManagementSystem.Core.Interfaces {
    /// <summary>
    /// Reusable version comparison service. All version comparisons in the system
    /// must go through this service — never raw string comparison.
    /// </summary>
    public interface IVersionService {
        SemanticVersion Parse(string version);

        bool TryParse(string version, out SemanticVersion? parsed);

        /// <summary>Returns &lt;0 if left is older, 0 if equal, &gt;0 if left is newer than right.</summary>
        int Compare(string left, string right);

        bool IsNewer(string candidate, string baseline);

        bool IsOlder(string candidate, string baseline);

        bool IsEqual(string left, string right);
    }
}
