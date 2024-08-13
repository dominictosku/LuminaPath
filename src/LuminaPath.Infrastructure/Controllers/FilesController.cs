using LuminaPath.Core.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LuminaPath.Infrastructure.Controllers
{
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class FilesController : ControllerBase
	{
		private readonly ILogger<FilesController> _logger;
		private readonly IAzureStorage Storage;

		public FilesController(ILogger<FilesController> logger, IAzureStorage azureStorage)
		{
			_logger = logger; ;
			Storage = azureStorage;
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> PostImage(IFormFile file)
		{
			var result = await Storage.UploadAsync(file);
			return Ok(new { Message = "File uploaded successfully." });
		}

		[HttpGet("{url}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetImage(string url)
		{
			var result = await Storage.DownloadAsync(url);
			if (result is null || result.Content is null)
				return File("/images/placeholder.png", "images/png");
			return new FileStreamResult(result.Content, result.ContentType ?? "images/png");
		}
	}
}
