namespace LuminaPath.Infrastructure.Services.Storage
{
    internal static class StorageFileNames
    {
        public static string GetSafeName(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(safeFileName)
                || safeFileName is "." or "..")
            {
                throw new InvalidOperationException("File name is required.");
            }

            return safeFileName;
        }

        public static string WithExtensionFromContentType(string fileName, string? contentType)
        {
            var safeFileName = GetSafeName(fileName);

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(safeFileName)))
            {
                return safeFileName;
            }

            var extension = StorageContentTypes.GetExtensionFromContentType(contentType);
            return extension is null
                ? safeFileName
                : $"{safeFileName}{extension}";
        }

        public static string ForRename(string oldPath, string newName)
        {
            var safeNewName = GetSafeName(newName);

            if (!string.IsNullOrWhiteSpace(Path.GetExtension(safeNewName)))
            {
                return safeNewName;
            }

            return $"{safeNewName}{Path.GetExtension(oldPath)}";
        }
    }
}
