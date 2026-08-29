using System;
using System.Drawing;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using VersionManagementSystem.Core.DTOs;

namespace VersionManagementSystem.Admin {
    /// <summary>
    /// Minimal admin dashboard: four summary counts pulled from GET /api/dashboard/summary,
    /// matching the numbers shown in the spec's dashboard mockup (section 17). This is
    /// intentionally the smallest useful starting point — the Applications/Versions/Releases/
    /// Update Packages/Clients/Update History/Settings screens from sections 17-19 are their
    /// own (larger) pieces of work built the same way: call the existing REST endpoints and
    /// bind the results to a grid.
    /// </summary>
    public sealed class DashboardForm : Form {
        private readonly HttpClient _httpClient;
        private readonly Label _applicationsValue = CreateValueLabel();
        private readonly Label _publishedValue = CreateValueLabel();
        private readonly Label _pendingValue = CreateValueLabel();
        private readonly Label _updatesTodayValue = CreateValueLabel();
        private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.DimGray };

        public DashboardForm(string apiBaseUrl) {
            _httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };

            Text = "Version Management System — Dashboard";
            Width = 460;
            Height = 320;
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            _ = LoadSummaryAsync();
        }

        private void BuildLayout() {
            var title = new Label {
                Text = "Version Management System",
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };
            Controls.Add(title);

            var grid = new TableLayoutPanel {
                ColumnCount = 2,
                RowCount = 4,
                Location = new Point(20, 60),
                Size = new Size(400, 160),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };

            AddRow(grid, "Applications", _applicationsValue);
            AddRow(grid, "Published Versions", _publishedValue);
            AddRow(grid, "Pending Releases", _pendingValue);
            AddRow(grid, "Updates Today", _updatesTodayValue);

            Controls.Add(grid);

            var refreshButton = new Button {
                Text = "Refresh",
                Location = new Point(20, 235),
                Width = 100
            };
            refreshButton.Click += async (_, _) => await LoadSummaryAsync();
            Controls.Add(refreshButton);

            _statusLabel.Location = new Point(130, 240);
            Controls.Add(_statusLabel);
        }

        private static void AddRow(TableLayoutPanel grid, string labelText, Label valueLabel) {
            grid.Controls.Add(new Label { Text = labelText, AutoSize = true, Padding = new Padding(8) });
            grid.Controls.Add(valueLabel);
        }

        private static Label CreateValueLabel() => new() {
            Text = "—",
            AutoSize = true,
            Padding = new Padding(8),
            Font = new Font(Control.DefaultFont, FontStyle.Bold)
        };

        private async Task LoadSummaryAsync() {
            _statusLabel.Text = "Loading...";
            try {
                var summary = await _httpClient.GetFromJsonAsync<DashboardSummaryDTO>("dashboard/summary");
                if (summary is null) {
                    _statusLabel.Text = "No data returned.";
                    return;
                }

                _applicationsValue.Text = summary.Applications.ToString();
                _publishedValue.Text = summary.PublishedVersions.ToString();
                _pendingValue.Text = summary.PendingReleases.ToString();
                _updatesTodayValue.Text = summary.UpdatesToday.ToString();
                _statusLabel.Text = $"Last refreshed {DateTime.Now:t}";
            }
            catch (Exception ex) {
                _statusLabel.Text = "Failed to load.";
                MessageBox.Show(this, ex.Message, "Dashboard load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
