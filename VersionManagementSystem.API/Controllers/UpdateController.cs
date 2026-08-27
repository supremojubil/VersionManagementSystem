using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Enums;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    [Route("api/update")]
    public class UpdateController : ControllerBase {
        private readonly IUpdateCheckService _updateCheckService;
        private readonly IClientTrackingService _clientTrackingService;
        public UpdateController(IUpdateCheckService updateCheckService, IClientTrackingService clientTrackingService) {
            _updateCheckService = updateCheckService;
            _clientTrackingService = clientTrackingService;
        }

        // GET /api/update/check?application=FJ&version=1.4.0&channel=Stable&machineName=WKSTN-01
        [HttpGet("check")]
        public async Task<IActionResult> Check([FromQuery] string application, [FromQuery] string version, [FromQuery] string channel = "Stable", [FromQuery] string? machineName = null) {
            if (string.IsNullOrWhiteSpace(application) || string.IsNullOrWhiteSpace(version)) {
                return BadRequest(new { message = "Both 'application' and 'version' query parameters are required." });
            }

            if (!Enum.TryParse<UpdateChannel>(channel, ignoreCase: true, out var parsedChannel)) {
                return BadRequest(new { message = $"'{channel}' is not a valid channel. Expected Stable, Beta or Development." });
            }

            try {
                var result = await _updateCheckService.CheckForUpdateAsync(application, version, parsedChannel, machineName);
                return Ok(result);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/update/latest?application=FJ&channel=Stable
        [HttpGet("latest")]
        public async Task<IActionResult> Latest([FromQuery] string application, [FromQuery] string channel = "Stable") {
            if (string.IsNullOrWhiteSpace(application)) {
                return BadRequest(new { message = "'application' query parameter is required." });
            }

            if (!Enum.TryParse<UpdateChannel>(channel, ignoreCase: true, out var parsedChannel)) {
                return BadRequest(new { message = $"'{channel}' is not a valid channel. Expected Stable, Beta or Development." });
            }

            try {
                var result = await _updateCheckService.GetLatestAsync(application, parsedChannel);
                return Ok(result);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST /api/update/history — a client reports the outcome of an update attempt.
        [HttpPost("history")]
        public async Task<IActionResult> RecordHistory([FromBody] RecordUpdateHistoryDTO request) {
            try {
                var result = await _clientTrackingService.RecordUpdateHistoryAsync(request);
                return Ok(result);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
