using LuminaPath.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LuminaPath.Infrastructure.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService Storage;

        public FilesController(IStorageService azureStorage)
        {
            Storage = azureStorage;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PostImage(IFormFile file)
        {
            var result = await Storage.UploadAsync(file);
            if (result.Error)
            {
                return BadRequest(result.Status);
            }

            return Ok(new { Message = "File uploaded successfully." });
        }

        [HttpGet("{url}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(string url)
        {
            var result = await Storage.DownloadAsync(url);
            if (result is null || result.Content is null)
                return File("/images/placeholder.png", "image/png");
            return new FileStreamResult(result.Content, result.ContentType ?? "image/png");
        }
    }
}
