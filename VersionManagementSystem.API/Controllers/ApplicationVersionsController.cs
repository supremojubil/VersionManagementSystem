using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    [Route("api/applications/{applicationCode}/versions")]
    public class ApplicationVersionsController : ControllerBase {
        private readonly IApplicationVersionService _versionService;

        public ApplicationVersionsController(IApplicationVersionService versionService) {
            _versionService = versionService;
        }

        // GET /api/applications/{applicationCode}/versions
        [HttpGet]
        public async Task<IActionResult> GetHistory(string applicationCode) {
            try {
                var history = await _versionService.GetHistoryAsync(applicationCode);
                return Ok(history);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // GET /api/applications/{applicationCode}/versions/latest
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(string applicationCode) {
            try {
                var latest = await _versionService.GetLatestAsync(applicationCode);
                return Ok(latest);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST /api/applications/{applicationCode}/versions
        [HttpPost]
        public async Task<IActionResult> Create(string applicationCode, [FromBody] CreateApplicationVersionDTO request) {
            request.ApplicationCode = applicationCode;

            try {
                var created = await _versionService.CreateAsync(request);
                return CreatedAtAction(nameof(GetHistory), new { applicationCode }, created);
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
