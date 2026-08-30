using System;

namespace VersionManagementSystem.Core.Interfaces {
    /// <summary>Reusable validation and comparison service for .NET assembly versions.</summary>
    public interface IVersionService {
        Version Parse(string version);
        bool TryParse(string version, out Version? parsed);
        int Compare(string left, string right);
        bool IsNewer(string candidate, string baseline);
        bool IsOlder(string candidate, string baseline);
        bool IsEqual(string left, string right);
    }
}
