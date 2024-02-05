using Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
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
		public async Task<FileStreamResult> GetImage(string url)
		{
			var result = await Storage.DownloadAsync(url);
			return new FileStreamResult(result.Content, result.ContentType);
		}
	}
}
