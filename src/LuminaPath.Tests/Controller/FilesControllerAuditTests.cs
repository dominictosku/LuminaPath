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

public class FilesControllerAuditTests
{
    [Fact]
    public async Task GetImage_WithDownloadQuery_RecordsDocumentDownloadedAudit()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var storage = new Mock<IStorageService>();
        storage
            .Setup(service => service.DownloadAsync("guide.pdf"))
            .ReturnsAsync(new BlobDto
            {
                Content = new MemoryStream([1, 2, 3]),
                ContentType = "application/pdf"
            });

        var controller = CreateController(options, storage);
        controller.ControllerContext.HttpContext.Request.QueryString = new QueryString("?download=true");

        var result = await controller.GetImage("guide.pdf");

        Assert.IsType<FileStreamResult>(result);
        await using var assertContext = new LuminaPathDbContext(options);
        var auditLog = Assert.Single(await assertContext.AuditLogs
            .Where(log => log.Action == AuditActions.DocumentDownloaded)
            .ToListAsync());
        Assert.Equal(AuditCategories.Document, auditLog.Category);
        Assert.Equal(AuditOutcomes.Success, auditLog.Outcome);
        Assert.Equal("guide.pdf", auditLog.TargetName);
    }

    [Fact]
    public async Task GetImage_ForImagePreviewWithoutDownloadQuery_DoesNotRecordAudit()
    {
        var options = Utilities.DbContext.TestDbContextOptions();
        var storage = new Mock<IStorageService>();
        storage
            .Setup(service => service.DownloadAsync("cover.png"))
            .ReturnsAsync(new BlobDto
            {
                Content = new MemoryStream([1, 2, 3]),
                ContentType = "image/png"
            });

        var controller = CreateController(options, storage);

        var result = await controller.GetImage("cover.png");

        Assert.IsType<FileStreamResult>(result);
        await using var assertContext = new LuminaPathDbContext(options);
        Assert.Empty(await assertContext.AuditLogs.ToListAsync());
    }

    private static FilesController CreateController(
        DbContextOptions<LuminaPathDbContext> options,
        Mock<IStorageService> storage)
    {
        return new FilesController(
            storage.Object,
            new AuditLogService(new TestDbContextFactory(options)))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
