namespace VersionManagementSystem.Core.Enums {
    /// <summary>
    /// Workflow: Draft -> Testing -> Approved -> Published -> Deprecated -> Archived.
    /// Only Published versions are exposed to client update checks.
    /// </summary>
    public enum ReleaseStatus {
        Draft = 0,
        Testing = 1,
        Approved = 2,
        Published = 3,
        Deprecated = 4,
        Archived = 5
    }
}
