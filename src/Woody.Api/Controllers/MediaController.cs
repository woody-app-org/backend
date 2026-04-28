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

    [AllowAnonymous]
    [HttpGet("images/{storageKey}")]
    [EnableRateLimiting(RateLimitPolicyNames.PublicRead)]
    public async Task<IActionResult> GetImage(string storageKey, CancellationToken cancellationToken)
    {
        var result = await _storage.OpenReadAsync(storageKey, cancellationToken);
        if (result == null)
            return NotFound();

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentDisposition = "inline; filename=\"image\"";
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        return File(result.Content, result.ContentType, enableRangeProcessing: false);
    }
}

public sealed class ImageUploadRequest
{
    public IFormFile? File { get; set; }
}
