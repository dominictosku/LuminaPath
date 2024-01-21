using Infrastructure.Dto.Blob;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Server.Services
{
    public class ImageHub
	{
		private readonly IAzureStorage _azureStorage;
		public ImageHub(IAzureStorage azureStorage)
		{
			_azureStorage = azureStorage;
		}

		public async Task<BlobResponseDto> GetImage(string fileName)
		{
			var image = await _azureStorage.DownloadAsync(fileName);
			if (image == null)
			{
				return new BlobResponseDto()
				{
					Error = true,
					Status = "Could no find image"
				};
			}
			var result = new BlobResponseDto()
			{
				Error = false,
				Status = "success",
				Blob = image
			};

			return result;
		}

		public async Task<BlobResponseDto> SaveImage(IFormFile file)
		{
			if (file == null)
			{
				return new BlobResponseDto()
				{
					Status = "File was null",
					Error = true
				};
			}
			string fileName = new Guid().ToString();
			var result = await _azureStorage.UploadAsync(file);
			return result;
		}
	}
}
