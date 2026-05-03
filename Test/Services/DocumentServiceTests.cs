using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Test.Utilities;

namespace Test.Services
{
    public class DocumentServiceTests
    {
        [Fact]
        public async Task CreateDocument_SanitizesFileName_AndInfersDocumentType()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.UploadAsync(
                    It.IsAny<Stream>(),
                    "cover_art.png",
                    "image/png"))
                .ReturnsAsync(new BlobResponseDto
                {
                    Blob = new BlobDto
                    {
                        Name = "cover_art.png",
                        ContentType = "image/png"
                    }
                });

            var service = CreateService(options, storage);
            var file = CreateBrowserFile("cover art.png", "image/png");

            var result = await service.CreateDocument(file);
            var document = result.Match<MediaDocument?>(success => success, failure => null);

            Assert.True(result.IsSuccess);
            Assert.NotNull(document);
            Assert.Equal("cover_art.png", document!.Name);
            Assert.Equal(DocumentType.Image, document.DocumentType);
            storage.VerifyAll();
        }

        [Fact]
        public async Task CreateMediaDocument_RejectsDocumentsWithoutFileOrExternalPath()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var service = CreateService(options);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateMediaDocument(new MediaDocument()));

            Assert.Equal("Document needs a file name or path.", exception.Message);
        }

        [Fact]
        public async Task UpdateMediaDocument_RenamesStoredFile_WhenNameChanges()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.MediaDocuments.Add(new MediaDocument
                {
                    Name = "old.pdf",
                    ContentType = "application/pdf",
                    DocumentType = DocumentType.PDF
                });
                await dbContext.SaveChangesAsync();
            }

            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.RenameAsync("old.pdf", "new.pdf"))
                .ReturnsAsync(true);

            var service = CreateService(options, storage);

            var updated = await service.UpdateMediaDocument(new MediaDocument
            {
                Id = 1,
                Name = "new.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.PDF
            });

            Assert.Equal("new.pdf", updated.Name);
            storage.Verify(service => service.RenameAsync("old.pdf", "new.pdf"), Times.Once);
        }

        [Fact]
        public async Task UpdateMediaDocument_DoesNotRenameExternalUrlDocuments()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.MediaDocuments.Add(new MediaDocument
                {
                    Name = "Docs",
                    Path = "https://example.com/docs",
                    DocumentType = DocumentType.Document
                });
                await dbContext.SaveChangesAsync();
            }

            var storage = new Mock<IStorageService>();
            var service = CreateService(options, storage);

            await service.UpdateMediaDocument(new MediaDocument
            {
                Id = 1,
                Name = "Docs v2",
                Path = "https://example.com/docs",
                DocumentType = DocumentType.Document
            });

            storage.Verify(service => service.RenameAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private static DocumentService CreateService(
            DbContextOptions<LuminaPathDbContext> options,
            Mock<IStorageService>? storage = null)
        {
            return new DocumentService(
                new TestDbContextFactory(options),
                storage?.Object ?? new Mock<IStorageService>().Object,
                new Mock<ILogger<DocumentService>>().Object);
        }

        private static IBrowserFile CreateBrowserFile(string name, string contentType)
        {
            var file = new Mock<IBrowserFile>();
            file.SetupGet(browserFile => browserFile.Name).Returns(name);
            file.SetupGet(browserFile => browserFile.ContentType).Returns(contentType);
            file.Setup(browserFile => browserFile.OpenReadStream(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() => new MemoryStream([1, 2, 3]));
            return file.Object;
        }
    }
}
