using LuminaPath.Core.Dtos;
using LuminaPath.Core.Interfaces;
using LuminaPath.Infrastructure.Services.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LuminaPath.Tests.Services.Storage;

public sealed class FallbackStorageServiceTests
{
    private readonly Mock<IStorageService> _primary = new();
    private readonly Mock<IStorageService> _fallback = new();
    private readonly FallbackStorageService _service;

    public FallbackStorageServiceTests()
    {
        _service = new FallbackStorageService(_primary.Object, _fallback.Object, NullLogger<FallbackStorageService>.Instance);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsPrimaryFile_WhenPresent()
    {
        var primaryBlob = new BlobDto { Name = "test.png" };
        _primary.Setup(p => p.DownloadAsync("test.png")).ReturnsAsync(primaryBlob);

        var result = await _service.DownloadAsync("test.png");

        Assert.Same(primaryBlob, result);
        _fallback.Verify(f => f.DownloadAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_FallsBack_WhenMissingOnPrimary()
    {
        var fallbackBlob = new BlobDto { Name = "test.png" };
        _primary.Setup(p => p.DownloadAsync("test.png")).ReturnsAsync((BlobDto?)null);
        _fallback.Setup(f => f.DownloadAsync("test.png")).ReturnsAsync(fallbackBlob);

        var result = await _service.DownloadAsync("test.png");

        Assert.Same(fallbackBlob, result);
        _primary.Verify(p => p.DownloadAsync("test.png"), Times.Once);
        _fallback.Verify(f => f.DownloadAsync("test.png"), Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_ReturnsNull_WhenMissingOnBoth()
    {
        _primary.Setup(p => p.DownloadAsync("missing.png")).ReturnsAsync((BlobDto?)null);
        _fallback.Setup(f => f.DownloadAsync("missing.png")).ReturnsAsync((BlobDto?)null);

        var result = await _service.DownloadAsync("missing.png");

        Assert.Null(result);
    }

    [Fact]
    public async Task UploadAsync_TargetsPrimaryOnly()
    {
        using var stream = new MemoryStream([1, 2, 3]);
        var response = new BlobResponseDto { Error = false };
        _primary.Setup(p => p.UploadAsync(stream, "upload.png", "image/png")).ReturnsAsync(response);

        var result = await _service.UploadAsync(stream, "upload.png", "image/png");

        Assert.Same(response, result);
        _fallback.Verify(f => f.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_TargetsPrimaryOnly()
    {
        var response = new BlobResponseDto { Error = false };
        _primary.Setup(p => p.DeleteAsync("del.png")).ReturnsAsync(response);

        var result = await _service.DeleteAsync("del.png");

        Assert.Same(response, result);
        _fallback.Verify(f => f.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_MergesDistinctFileNames()
    {
        _primary.Setup(p => p.ListAsync()).ReturnsAsync(new List<BlobDto>
        {
            new() { Name = "a.png" },
            new() { Name = "shared.png" }
        });
        _fallback.Setup(f => f.ListAsync()).ReturnsAsync(new List<BlobDto>
        {
            new() { Name = "shared.png" },
            new() { Name = "b.png" }
        });

        var result = await _service.ListAsync();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, b => b.Name == "a.png");
        Assert.Contains(result, b => b.Name == "shared.png");
        Assert.Contains(result, b => b.Name == "b.png");
    }
}
