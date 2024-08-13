namespace LuminaPath.Core.Common.Features.Files.Dto
{
	public class BlobResponseDto
	{
		public string? Status { get; set; }
		public bool Error { get; set; }
		public BlobDto Blob { get; set; }

		public BlobResponseDto()
		{
			Blob = new BlobDto();
		}
	}
}
