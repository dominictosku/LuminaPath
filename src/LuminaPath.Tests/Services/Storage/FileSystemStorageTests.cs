using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Services.Storage
{
    public sealed class FileSystemStorageTests : IDisposable
    {
        private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"luminapath-storage-{Guid.NewGuid():N}");

        [Fact]
        public async Task UploadAsync_AppendsExtensionFromContentType_WhenFileNameHasNoExtension()
        {
            var storage = CreateStorage();
            await using var content = new MemoryStream([1, 2, 3]);

            var result = await storage.UploadAsync(content, "cover", "image/png");

            Assert.False(result.Error);
            Assert.Equal("cover.png", result.Blob.Name);
            Assert.Equal("image/png", result.Blob.ContentType);
            Assert.True(File.Exists(Path.Combine(_storagePath, "cover.png")));
        }

        [Fact]
        public async Task DownloadAsync_UsesSafeFileName_AndDoesNotResolveOutsideStorageDirectory()
        {
            var storage = CreateStorage();
            await using var content = new MemoryStream([1, 2, 3]);
            await storage.UploadAsync(content, "cover.png", "image/png");

            var result = await storage.DownloadAsync(@"..\cover.png");

            Assert.NotNull(result);
            Assert.Equal("cover.png", result!.Name);
            Assert.Equal("image/png", result.ContentType);
            result.Content?.Dispose();
        }

        [Fact]
        public async Task RenameAsync_PreservesOldExtension_WhenNewNameHasNoExtension()
        {
            var storage = CreateStorage();
            await using var content = new MemoryStream([1, 2, 3]);
            await storage.UploadAsync(content, "cover.png", "image/png");

            var renamed = await storage.RenameAsync("cover.png", "hero");

            Assert.True(renamed);
            Assert.False(File.Exists(Path.Combine(_storagePath, "cover.png")));
            Assert.True(File.Exists(Path.Combine(_storagePath, "hero.png")));
        }

        [Fact]
        public async Task DeleteAsync_RemovesStoredFile()
        {
            var storage = CreateStorage();
            await using var content = new MemoryStream([1, 2, 3]);
            await storage.UploadAsync(content, "cover.png", "image/png");

            var result = await storage.DeleteAsync("cover.png");

            Assert.False(result.Error);
            Assert.False(File.Exists(Path.Combine(_storagePath, "cover.png")));
        }

        public void Dispose()
        {
            if (Directory.Exists(_storagePath))
            {
                Directory.Delete(_storagePath, recursive: true);
            }
        }

        private FileSystemStorage CreateStorage()
        {
            return new FileSystemStorage(_storagePath, new Mock<ILogger<FileSystemStorage>>().Object);
        }
    }
}
