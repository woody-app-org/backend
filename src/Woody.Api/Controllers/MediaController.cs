using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Application.Interfaces;
using Woody.Domain.Media;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaUploadService _uploads;
    private readonly IMediaStorage _storage;

    public MediaController(IMediaUploadService uploads, IMediaStorage storage)
    {
        _uploads = uploads;
        _storage = storage;
    }

    [Authorize]
    [HttpPost("images")]
    [EnableRateLimiting(RateLimitPolicyNames.Upload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadedImagePolicy.DefaultMaxSizeBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadedImagePolicy.DefaultMaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadImage([FromForm] ImageUploadRequest? request, CancellationToken cancellationToken)
    {
        if (request?.File == null)
            return BadRequest(new { error = "Arquivo obrigatório." });

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _uploads.UploadImageAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                cancellationToken);
            return Created(result.Url, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("videos")]
    [EnableRateLimiting(RateLimitPolicyNames.Upload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadedVideoPolicy.DefaultMaxSizeBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadedVideoPolicy.DefaultMaxSizeBytes + 1024 * 1024)]
    public async Task<IActionResult> UploadVideo([FromForm] VideoUploadRequest? request, CancellationToken cancellationToken)
    {
        if (request?.File == null)
            return BadRequest(new { error = "Arquivo obrigatório." });

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _uploads.UploadVideoAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                request.File.Length,
                cancellationToken);
            return Created(result.Url, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("images/{storageKey}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicRead)]
    public async Task<IActionResult> GetImage(string storageKey, CancellationToken cancellationToken) =>
        await ServeMediaAsync(storageKey, "image", enableRangeProcessing: false, cancellationToken);

    [AllowAnonymous]
    [HttpGet("videos/{storageKey}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicRead)]
    public async Task<IActionResult> GetVideo(string storageKey, CancellationToken cancellationToken) =>
        await ServeMediaAsync(storageKey, "video", enableRangeProcessing: true, cancellationToken);

    private async Task<IActionResult> ServeMediaAsync(
        string storageKey,
        string downloadName,
        bool enableRangeProcessing,
        CancellationToken cancellationToken)
    {
        var result = await _storage.OpenReadAsync(storageKey, cancellationToken);
        if (result == null)
            return NotFound();

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{downloadName}\"";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(result.Content, result.ContentType, enableRangeProcessing: enableRangeProcessing);
    }
}

public sealed class ImageUploadRequest
{
    public IFormFile? File { get; set; }
}

public sealed class VideoUploadRequest
{
    public IFormFile? File { get; set; }
}
