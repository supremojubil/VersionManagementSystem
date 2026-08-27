using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService) {
            _dashboardService = dashboardService;
        }

        // GET /api/dashboard/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary() {
            var summary = await _dashboardService.GetSummaryAsync();
            return Ok(summary);
        }
    }
}
