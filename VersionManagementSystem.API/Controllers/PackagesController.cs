using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VersionManagementSystem.Core.Exceptions;
using VersionManagementSystem.Core.Interfaces;

namespace VersionManagementSystem.API.Controllers {
    [ApiController]
    public class PackagesController : ControllerBase {
        private readonly IPackageService _packageService;

        public PackagesController(IPackageService packageService) {
            _packageService = packageService;
        }

        // POST /api/applications/{applicationCode}/versions/{version}/package
        // multipart/form-data with a single "file" field.
        [HttpPost("api/applications/{applicationCode}/versions/{version}/package")]
        [RequestSizeLimit(500L * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            string applicationCode, string version, IFormFile file, [FromQuery] string? uploadedBy) {
            if (file is null || file.Length == 0) {
                return BadRequest(new { message = "A non-empty package file is required." });
            }

            try {
                await using var stream = file.OpenReadStream();
                var result = await _packageService.UploadAsync(applicationCode, version, file.FileName, stream, uploadedBy);

                return CreatedAtAction(nameof(GetByVersion), new { applicationCode, version }, result);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/applications/{applicationCode}/versions/{version}/packages
        [HttpGet("api/applications/{applicationCode}/versions/{version}/packages")]
        public async Task<IActionResult> GetByVersion(string applicationCode, string version) {
            try {
                var packages = await _packageService.GetByVersionAsync(applicationCode, version);
                return Ok(packages);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/packages/{id}/download — the only way to fetch package bytes; never by raw path.
        [HttpGet("api/packages/{id:int}/download")]
        public async Task<IActionResult> Download(int id) {
            try {
                var (content, fileName, checksum) = await _packageService.GetDownloadAsync(id);
                Response.Headers["X-Checksum-SHA256"] = checksum;
                return File(content, "application/octet-stream", fileName);
            }
            catch (NotFoundException ex) {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
