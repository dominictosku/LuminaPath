using LuminaPath.Core.Dtos;
using LuminaPath.Core.Entities.Results;
using LuminaPath.Core.Enums;
using LuminaPath.Infrastructure.Identity;
using LuminaPath.Infrastructure.Services.ModelServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LuminaPath.Infrastructure.Controllers;

[ApiController]
[Authorize]
[Route("api/media-videos")]
public class MediaVideosController : ControllerBase
{
    private const long MaxVideoUploadRequestBytes = MediaVideoService.MaxVideoUploadBytes + (1024L * 1024L);

    private readonly MediaVideoService _mediaVideoService;

    public MediaVideosController(MediaVideoService mediaVideoService)
    {
        _mediaVideoService = mediaVideoService;
    }

    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public async Task<ActionResult<IReadOnlyList<MediaVideoDto>>> GetForMedia(
        [FromQuery] int mediaId,
        CancellationToken cancellationToken = default)
    {
        if (mediaId <= 0)
        {
            return BadRequest("Media id is required.");
        }

        var videos = await _mediaVideoService.GetForMediaAsync(mediaId, cancellationToken);
        return Ok(videos);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    [EnableRateLimiting(RateLimitPolicies.Uploads)]
    [RequestSizeLimit(MaxVideoUploadRequestBytes)]
    public async Task<ActionResult<MediaVideoDto>> Upload(
        [FromForm] int mediaId,
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] MediaVideoKind kind,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
        {
            return BadRequest("No video file was uploaded.");
        }

        if (file.Length > MediaVideoService.MaxVideoUploadBytes)
        {
            return new ObjectResult($"Video is too large. Maximum allowed size is {MediaVideoService.MaxVideoUploadBytes / 1024 / 1024} MiB.")
            {
                StatusCode = StatusCodes.Status413PayloadTooLarge,
            };
        }

        await using var stream = file.OpenReadStream();
        var result = await _mediaVideoService.UploadAsync(
            mediaId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            kind,
            title,
            description,
            cancellationToken);

        return result.Match<ActionResult<MediaVideoDto>>(
            video => CreatedAtAction(nameof(GetForMedia), new { mediaId = video.MediaId }, video),
            failure => BadRequest(FailureMessage(failure)));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public async Task<ActionResult<MediaVideoDto>> Update(
        int id,
        MediaVideoUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediaVideoService.UpdateAsync(id, update, cancellationToken);
        return result.Match<ActionResult<MediaVideoDto>>(
            video => Ok(video),
            failure => BadRequest(FailureMessage(failure)));
    }

    [HttpPut("reorder")]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public async Task<ActionResult<IReadOnlyList<MediaVideoDto>>> Reorder(
        MediaVideoReorderDto reorder,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediaVideoService.ReorderAsync(reorder, cancellationToken);
        return result.Match<ActionResult<IReadOnlyList<MediaVideoDto>>>(
            videos => Ok(videos),
            failure => BadRequest(FailureMessage(failure)));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.CatalogEditors)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _mediaVideoService.DeleteAsync(id, cancellationToken);
        return result.Match<IActionResult>(
            _ => NoContent(),
            failure => BadRequest(FailureMessage(failure)));
    }

    [HttpGet("{id:int}/stream")]
    [EnableRateLimiting(RateLimitPolicies.BroadReads)]
    public async Task<IActionResult> Stream(int id, CancellationToken cancellationToken = default)
    {
        var video = await _mediaVideoService.GetStreamAsync(id, cancellationToken);
        if (video is null)
        {
            return NotFound();
        }

        return File(video.Content, video.ContentType, enableRangeProcessing: true);
    }

    private static string FailureMessage(FailedResult failure)
    {
        return string.Join(" ", failure.errorMessage);
    }
}
