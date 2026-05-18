namespace LuminaPath.Infrastructure.Services.Storage
{
    internal static class StorageContentTypes
    {
        public static string GetContentType(string filePath)
        {
            return Path.GetExtension(filePath).ToLowerInvariant() switch
            {
                ".apng" => "image/apng",
                ".avif" => "image/avif",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".svg" => "image/svg+xml",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }

        public static string? GetExtensionFromContentType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return null;
            }

            var value = contentType.ToLowerInvariant();

            if (value.Contains(".apng") || value.Contains("apng"))
            {
                return ".apng";
            }

            if (value.Contains(".jpeg") || value.Contains("jpeg"))
            {
                return ".jpeg";
            }

            if (value.Contains(".jpg") || value.Contains("jpg"))
            {
                return ".jpg";
            }

            if (value.Contains(".png") || value.Contains("png"))
            {
                return ".png";
            }

            if (value.Contains(".webp") || value.Contains("webp"))
            {
                return ".webp";
            }

            if (value.Contains(".gif") || value.Contains("gif"))
            {
                return ".gif";
            }

            if (value.Contains(".bmp") || value.Contains("bmp"))
            {
                return ".bmp";
            }

            if (value.Contains(".avif") || value.Contains("avif"))
            {
                return ".avif";
            }

            if (value.Contains(".svg") || value.Contains("svg"))
            {
                return ".svg";
            }

            if (value.Contains(".pdf") || value.Contains("pdf"))
            {
                return ".pdf";
            }

            if (value.Contains(".txt") || value.Contains("text/plain"))
            {
                return ".txt";
            }

            return null;
        }
    }
}
