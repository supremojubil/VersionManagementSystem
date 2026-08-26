using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    [Route("api/applications/{applicationCode}/versions/{version}")]
    public class ReleasesController : ControllerBase {
        private readonly IReleaseService _releaseService;

        public ReleasesController(IReleaseService releaseService) {
            _releaseService = releaseService;
        }

        // POST /api/applications/{applicationCode}/versions/{version}/submit-for-testing
        [HttpPost("submit-for-testing")]
        public Task<IActionResult> SubmitForTesting(string applicationCode, string version) => RunTransition(() => _releaseService.SubmitForTestingAsync(applicationCode, version));

        // POST /api/applications/{applicationCode}/versions/{version}/approve
        [HttpPost("approve")]
        public Task<IActionResult> Approve(string applicationCode, string version) => RunTransition(() => _releaseService.ApproveAsync(applicationCode, version));

        // POST /api/applications/{applicationCode}/versions/{version}/publish
        [HttpPost("publish")]
        public Task<IActionResult> Publish(string applicationCode, string version, [FromQuery] string? publishedBy) => RunTransition(() => _releaseService.PublishAsync(applicationCode, version, publishedBy));

        // POST /api/applications/{applicationCode}/versions/{version}/deprecate
        [HttpPost("deprecate")]
        public Task<IActionResult> Deprecate(string applicationCode, string version) => RunTransition(() => _releaseService.DeprecateAsync(applicationCode, version));

        // POST /api/applications/{applicationCode}/versions/{version}/archive
        [HttpPost("archive")]
        public Task<IActionResult> Archive(string applicationCode, string version) => RunTransition(() => _releaseService.ArchiveAsync(applicationCode, version));

        // POST /api/applications/{applicationCode}/versions/{version}/release-notes
        [HttpPost("release-notes")]
        public async Task<IActionResult> AddReleaseNotes(string applicationCode, string version, [FromBody] List<CreateReleaseNoteDTO> releaseNotes) {
            try {
                var created = await _releaseService.AddReleaseNotesAsync(applicationCode, version, releaseNotes);
                return Ok(created);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static async Task<IActionResult> RunTransition(System.Func<Task<ApplicationVersionDTO>> action) {
            try {
                var result = await action();
                return new OkObjectResult(result);
            }
            catch (NotFoundException ex) {
                return new NotFoundObjectResult(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return new BadRequestObjectResult(new { message = ex.Message });
            }
        }
    }
}
