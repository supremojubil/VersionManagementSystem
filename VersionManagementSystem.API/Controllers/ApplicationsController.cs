using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.DTOs;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase {
        private readonly IApplicationService _applicationService;

        public ApplicationsController(IApplicationService applicationService) {
            _applicationService = applicationService;
        }

        // GET /api/applications
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false) {
            var applications = await _applicationService.GetAllAsync(includeInactive);
            return Ok(applications);
        }

        // GET /api/applications/{applicationCode}
        [HttpGet("{applicationCode}")]
        public async Task<IActionResult> GetByCode(string applicationCode) {
            try {
                var application = await _applicationService.GetByCodeAsync(applicationCode);
                return Ok(application);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }

        // POST /api/applications
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApplicationDTO request) {
            try {
                var created = await _applicationService.CreateAsync(request);
                return CreatedAtAction(nameof(GetByCode), new { applicationCode = created.ApplicationCode }, created);
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT /api/applications/{applicationCode}
        [HttpPut("{applicationCode}")]
        public async Task<IActionResult> Update(string applicationCode, [FromBody] UpdateApplicationDTO request) {
            try {
                var updated = await _applicationService.UpdateAsync(applicationCode, request);
                return Ok(updated);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE /api/applications/{applicationCode} — soft-disable, never a hard delete.
        [HttpDelete("{applicationCode}")]
        public async Task<IActionResult> Disable(string applicationCode) {
            try {
                await _applicationService.DisableAsync(applicationCode);
                return NoContent();
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
