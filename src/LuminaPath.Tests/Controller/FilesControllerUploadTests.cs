using System.Text;
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
/// auth (handled by ASP.NET, not here), non-empty file, raster image
/// signatures, and size cap. Storage is never touched when validation fails.
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
    public async Task PostImage_RejectsUnknownFileSignature_With415_AndNoStorageCall()
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
    public async Task PostImage_RejectsSvgUpload_With415_AndNoStorageCall()
    {
        var (controller, storage, options) = CreateController();
        var file = MakeFile(
            "payload.svg",
            "image/svg+xml",
            Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"));

        var result = await controller.PostImage(file);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status415UnsupportedMediaType, objectResult.StatusCode);
        storage.Verify(
            s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()),
            Times.Never);

        await AssertAuditFailureRecorded(options);
    }

    [Fact]
    public async Task PostImage_RejectsSpoofedRasterContentType_With415_AndNoStorageCall()
    {
        var (controller, storage, options) = CreateController();
        var file = MakeFile("payload.png", "image/png", Encoding.UTF8.GetBytes("not really a png"));

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
            .Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsRegex("^[a-f0-9]{32}\\.png$"),
                "image/png"))
            .ReturnsAsync((Stream _, string storageName, string? contentType) => new BlobResponseDto
            {
                Error = false,
                Blob = new BlobDto
                {
                    Name = storageName,
                    ContentType = contentType,
                }
            });

        var file = MakeFile("cover.png", "image/png", PngPayload());
        var result = await controller.PostImage(file);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var payload = ok.Value!;
        var type = payload.GetType();
        var storageName = Assert.IsType<string>(type.GetProperty("StorageName")!.GetValue(payload));
        Assert.Matches("^[a-f0-9]{32}\\.png$", storageName);
        Assert.NotEqual("cover.png", storageName);
        Assert.Equal("image/png", type.GetProperty("ContentType")!.GetValue(payload));
        Assert.Equal($"api/files/{storageName}", type.GetProperty("Url")!.GetValue(payload));

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
        return MakeFile(name, contentType, bytes);
    }

    private static IFormFile MakeFile(string name, string contentType, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, name, name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static byte[] PngPayload()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52
        ];
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
