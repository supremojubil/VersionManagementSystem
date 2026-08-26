namespace VersionManagementSystem.Infrastructure.Storage {
    public sealed class PackageStorageOptions {
        /// <summary>
        /// Root directory where package files are stored. Can be relative
        /// (resolved against the app's content root) or absolute.
        /// </summary>
        public string RootPath { get; set; } = "App_Data/Packages";
    }
}
