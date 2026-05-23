using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure;
using LuminaPath.Infrastructure.Controllers;
using LuminaPath.Infrastructure.Services.Auditing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Test.Utilities;

namespace Test.Controller;

/// <summary>
/// Covers the upload-validation gates added to <see cref="FilesController.PostImage"/>:
/// auth (handled by ASP.NET, not here), non-empty file, image content
/// type, and size cap. Storage is never touched when validation fails.
/// </summary>
public class FilesControllerUploadTests
{
    [Fact]
    public async Task PostImage_RejectsEmptyUpload_WithoutHittingStorage()
    {
        var (controller, storage, options) = CreateController();

        var result = await controller.PostImage(null);

        Assert.IsType<BadRequestObjectResult>(result);
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);

        await AssertAuditFailureRecorded(options);
    }

    [Fact]
    public async Task PostImage_RejectsNonImageContentType_With415_AndNoStorageCall()
    {
        var (controller, storage, options) = CreateController();
        var file = MakeFile("payload.zip", "application/zip", payloadSize: 16);

        var result = await controller.PostImage(file);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, objectResult.StatusCode);
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);

        await AssertAuditFailureRecorded(options);
    }

    [Fact]
    public async Task PostImage_RejectsFilesOverTheSizeCap_With413_AndNoStorageCall()
    {
        var (controller, storage, options) = CreateController();
        // 3 MiB + 1 byte
        var file = MakeFile("cover.png", "image/png", payloadSize: (3 * 1024 * 1024) + 1);

        var result = await controller.PostImage(file);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, objectResult.StatusCode);
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);

        await AssertAuditFailureRecorded(options);
    }

    [Fact]
    public async Task PostImage_AcceptsValidImage_AndReturnsBlobMetadata()
    {
        var (controller, storage, options) = CreateController();
        storage
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), "cover.png", "image/png"))
            .ReturnsAsync(new BlobResponseDto
            {
                Error = false,
                Blob = new BlobDto
                {
                    Name = "abc.png",
                    ContentType = "image/png",
                }
            });

        var file = MakeFile("cover.png", "image/png", payloadSize: 64);
        var result = await controller.PostImage(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var type = payload.GetType();
        Assert.Equal("abc.png", type.GetProperty("StorageName")!.GetValue(payload));
        Assert.Equal("image/png", type.GetProperty("ContentType")!.GetValue(payload));
        Assert.Equal("api/files/abc.png", type.GetProperty("Url")!.GetValue(payload));

        await using var assertContext = new LuminaPathDbContext(options);
        var auditLog = Assert.Single(await assertContext.AuditLogs.ToListAsync());
        Assert.Equal(AuditOutcomes.Success, auditLog.Outcome);
        Assert.Equal(AuditActions.DocumentUploaded, auditLog.Action);
    }

    private static (FilesController Controller, Mock<IStorageService> Storage, DbContextOptions<LuminaPathDbContext> Options) CreateController()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var storage = new Mock<IStorageService>(MockBehavior.Strict);
        var controller = new FilesController(
            storage.Object,
            new AuditLogService(new TestDbContextFactory(options)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        return (controller, storage, options);
    }

    private static IFormFile MakeFile(string name, string contentType, int payloadSize)
    {
        var bytes = new byte[payloadSize];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i % 256);
        }
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, name, name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static async Task AssertAuditFailureRecorded(DbContextOptions<LuminaPathDbContext> options)
    {
        await using var assertContext = new LuminaPathDbContext(options);
        var auditLog = Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == AuditActions.DocumentUploaded)
            .ToListAsync());
        Assert.Equal(AuditOutcomes.Failure, auditLog.Outcome);
    }
}
