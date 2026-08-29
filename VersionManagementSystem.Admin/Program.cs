using System;
using System.Windows.Forms;

namespace VersionManagementSystem.Admin {
    internal static class Program {
        [STAThread]
        private static void Main(string[] args) {
            ApplicationConfiguration.Initialize();

            // First arg lets an operator point the dashboard at a non-default API instance;
            // defaults to the typical local dev address.
            var apiBaseUrl = args.Length > 0 ? args[0] : "https://localhost:5001/api";

            Application.Run(new DashboardForm(apiBaseUrl));
        }
    }
}
