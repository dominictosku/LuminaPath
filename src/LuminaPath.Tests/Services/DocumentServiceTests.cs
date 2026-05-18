using LuminaPath.Core.Dtos;
using LuminaPath.Core.Enums;
using LuminaPath.Core.Interfaces;
using LuminaPath.Core.Models;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Services.Auditing;
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
        public async Task CreateDocument_PreservesDisplayName_UsesUniqueStorageName_AndInfersDocumentType()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.UploadAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(name => name.EndsWith(".png") && name != "cover art.png"),
                    "image/png"))
                .ReturnsAsync((Stream _, string storageName, string? _) => new BlobResponseDto
                {
                    Blob = new BlobDto
                    {
                        Name = storageName,
                        ContentType = "image/png"
                    }
                });

            var service = CreateService(options, storage);
            var file = CreateBrowserFile("cover art.png", "image/png");

            var result = await service.CreateDocument(file);
            var document = result.Match<MediaDocument?>(success => success, failure => null);

            Assert.True(result.IsSuccess);
            Assert.NotNull(document);
            Assert.Equal("cover art.png", document!.Name);
            Assert.NotEqual(document.Name, document.StorageName);
            Assert.EndsWith(".png", document.StorageName);
            Assert.Equal(DocumentType.Image, document.DocumentType);
            storage.VerifyAll();
        }

        [Fact]
        public async Task CreateDocument_FromStream_PreservesDisplayName_AndUsesUniqueStorageName()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.UploadAsync(
                    It.IsAny<Stream>(),
                    It.Is<string>(name => name.EndsWith(".jpg") && name != "provider cover.jpg"),
                    "image/jpeg"))
                .ReturnsAsync((Stream _, string storageName, string? _) => new BlobResponseDto
                {
                    Blob = new BlobDto
                    {
                        Name = storageName,
                        ContentType = "image/jpeg"
                    }
                });

            var service = CreateService(options, storage);
            await using var stream = new MemoryStream([1, 2, 3]);

            var result = await service.CreateDocument(stream, "provider cover.jpg", "image/jpeg");
            var document = result.Match<MediaDocument?>(success => success, failure => null);

            Assert.True(result.IsSuccess);
            Assert.NotNull(document);
            Assert.Equal("provider cover.jpg", document!.Name);
            Assert.NotEqual(document.Name, document.StorageName);
            Assert.EndsWith(".jpg", document.StorageName);
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
        public async Task CreateMediaDocument_RecordsDocumentCreatedAudit()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            var factory = new TestDbContextFactory(options);
            var service = CreateService(options, auditLog: new AuditLogService(factory));

            var document = await service.CreateMediaDocument(new MediaDocument
            {
                Name = "guide.pdf",
                StorageName = "stored-guide.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.PDF
            });

            await using var assertContext = new LuminaPathDbContext(options);
            var auditLog = Assert.Single(await assertContext.AuditLogs
                .Where(log => log.Action == AuditActions.DocumentCreated)
                .ToListAsync());
            Assert.Equal(AuditCategories.Document, auditLog.Category);
            Assert.Equal(document.Id.ToString(), auditLog.TargetId);
            Assert.Equal("guide.pdf", auditLog.TargetName);
            Assert.Equal(AuditOutcomes.Success, auditLog.Outcome);
        }

        [Fact]
        public async Task UpdateMediaDocument_UpdatesDisplayNameWithoutRenamingStoredFile()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.MediaDocuments.Add(new MediaDocument
                {
                    Name = "old.pdf",
                    StorageName = "stored.pdf",
                    ContentType = "application/pdf",
                    DocumentType = DocumentType.PDF
                });
                await dbContext.SaveChangesAsync();
            }

            var storage = new Mock<IStorageService>();
            var service = CreateService(options, storage);

            var updated = await service.UpdateMediaDocument(new MediaDocument
            {
                Id = 1,
                Name = "new.pdf",
                StorageName = "stored.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.PDF
            });

            Assert.Equal("new.pdf", updated.Name);
            Assert.Equal("stored.pdf", updated.StorageName);
            storage.Verify(service => service.RenameAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateMediaDocument_RecordsDocumentUpdatedAuditWithChanges()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                dbContext.MediaDocuments.Add(new MediaDocument
                {
                    Name = "old.pdf",
                    StorageName = "stored.pdf",
                    ContentType = "application/pdf",
                    DocumentType = DocumentType.PDF
                });
                await dbContext.SaveChangesAsync();
            }

            var factory = new TestDbContextFactory(options);
            var service = CreateService(options, auditLog: new AuditLogService(factory));

            await service.UpdateMediaDocument(new MediaDocument
            {
                Id = 1,
                Name = "new.pdf",
                StorageName = "stored.pdf",
                ContentType = "application/pdf",
                DocumentType = DocumentType.PDF
            });

            await using var assertContext = new LuminaPathDbContext(options);
            var auditLog = Assert.Single(await assertContext.AuditLogs
                .Where(log => log.Action == AuditActions.DocumentUpdated)
                .ToListAsync());
            Assert.Equal(AuditCategories.Document, auditLog.Category);
            Assert.Contains("Name", auditLog.ChangesJson);
            Assert.Contains("old.pdf", auditLog.ChangesJson);
            Assert.Contains("new.pdf", auditLog.ChangesJson);
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

        [Fact]
        public async Task DeleteDocument_RemovesDatabaseDocument_WhenStoredFileIsAlreadyMissing()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            int documentId;
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                var document = new MediaDocument
                {
                    Name = "cover.png",
                    StorageName = "stored-cover.png",
                    ContentType = "image/png",
                    DocumentType = DocumentType.Image
                };
                dbContext.MediaDocuments.Add(document);
                await dbContext.SaveChangesAsync();
                documentId = document.Id;
            }

            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.DeleteAsync("stored-cover.png"))
                .ReturnsAsync(new BlobResponseDto { Error = true, Status = "missing" });

            var service = CreateService(options, storage);
            await service.DeleteDocument(new MediaDocument { Id = documentId });

            await using var assertContext = new LuminaPathDbContext(options);
            Assert.Empty(await assertContext.MediaDocuments.ToListAsync());
            storage.Verify(service => service.DeleteAsync("stored-cover.png"), Times.Once);
        }

        [Fact]
        public async Task DeleteDocument_RecordsDocumentDeletedAudit()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            int documentId;
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                var document = new MediaDocument
                {
                    Name = "cover.png",
                    StorageName = "stored-cover.png",
                    ContentType = "image/png",
                    DocumentType = DocumentType.Image
                };
                dbContext.MediaDocuments.Add(document);
                await dbContext.SaveChangesAsync();
                documentId = document.Id;
            }

            var storage = new Mock<IStorageService>();
            storage
                .Setup(service => service.DeleteAsync("stored-cover.png"))
                .ReturnsAsync(new BlobResponseDto());
            var factory = new TestDbContextFactory(options);
            var service = CreateService(options, storage, new AuditLogService(factory));

            await service.DeleteDocument(new MediaDocument { Id = documentId });

            await using var assertContext = new LuminaPathDbContext(options);
            var auditLog = Assert.Single(await assertContext.AuditLogs
                .Where(log => log.Action == AuditActions.DocumentDeleted)
                .ToListAsync());
            Assert.Equal(AuditCategories.Document, auditLog.Category);
            Assert.Equal(documentId.ToString(), auditLog.TargetId);
            Assert.Equal("cover.png", auditLog.TargetName);
            Assert.Contains("Deleted", auditLog.MetadataJson);
        }

        [Fact]
        public async Task DeleteDocument_DoesNotDeleteStoredFile_WhenAnotherDocumentReferencesIt()
        {
            var options = Utilities.DbContext.TestDbContextOptions();
            int documentId;
            await using (var dbContext = new LuminaPathDbContext(options))
            {
                var first = new MediaDocument
                {
                    Name = "cover.png",
                    StorageName = "shared.png",
                    ContentType = "image/png",
                    DocumentType = DocumentType.Image
                };
                var second = new MediaDocument
                {
                    Name = "cover copy.png",
                    StorageName = "shared.png",
                    ContentType = "image/png",
                    DocumentType = DocumentType.Image
                };
                dbContext.MediaDocuments.AddRange(first, second);
                await dbContext.SaveChangesAsync();
                documentId = first.Id;
            }

            var storage = new Mock<IStorageService>();
            var service = CreateService(options, storage);
            await service.DeleteDocument(new MediaDocument { Id = documentId });

            await using var assertContext = new LuminaPathDbContext(options);
            var remaining = Assert.Single(await assertContext.MediaDocuments.ToListAsync());
            Assert.Equal("shared.png", remaining.StorageName);
            storage.Verify(service => service.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        private static DocumentService CreateService(
            DbContextOptions<LuminaPathDbContext> options,
            Mock<IStorageService>? storage = null,
            AuditLogService? auditLog = null)
        {
            return new DocumentService(
                new TestDbContextFactory(options),
                storage?.Object ?? new Mock<IStorageService>().Object,
                new Mock<ILogger<DocumentService>>().Object,
                auditLog);
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
