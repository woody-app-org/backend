using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.Exceptions;
using Woody.Application.Interfaces;
using Woody.Application.Media;
using Woody.Domain.Media;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private const long MultipartSlackBytes = 1024 * 1024;

    private readonly IMediaUploadApplicationService _uploads;
    private readonly IMediaStorage _storage;

    public MediaController(IMediaUploadApplicationService uploads, IMediaStorage storage)
    {
        _uploads = uploads;
        _storage = storage;
    }

    [Authorize]
    [HttpPost("images")]
    [EnableRateLimiting(RateLimitPolicyNames.Upload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaReferenceConstraints.ImageMaxUploadBytes + MultipartSlackBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MediaReferenceConstraints.ImageMaxUploadBytes + MultipartSlackBytes)]
    public async Task<IActionResult> UploadImage([FromForm] ScopedMediaUploadForm? request, CancellationToken cancellationToken)
    {
        if (request?.File == null)
            return BadRequest(new { error = "Arquivo obrigatório." });

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!TryBuildAuthorizationContext(request, userId.Value, out var auth, out var parseError))
            return BadRequest(new { error = parseError });

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _uploads.UploadImageAsync(
                auth,
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
        catch (MediaUploadForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("videos")]
    [EnableRateLimiting(RateLimitPolicyNames.Upload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MediaReferenceConstraints.PostVideoMaxUploadBytes + MultipartSlackBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MediaReferenceConstraints.PostVideoMaxUploadBytes + MultipartSlackBytes)]
    public async Task<IActionResult> UploadVideo([FromForm] ScopedMediaUploadForm? request, CancellationToken cancellationToken)
    {
        if (request?.File == null)
            return BadRequest(new { error = "Arquivo obrigatório." });

        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        if (!TryBuildAuthorizationContext(request, userId.Value, out var auth, out var parseError))
            return BadRequest(new { error = parseError });

        var authWithDuration = auth with { DeclaredDurationSeconds = request.DurationSeconds };

        try
        {
            await using var stream = request.File.OpenReadStream();
            var result = await _uploads.UploadVideoAsync(
                authWithDuration,
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
        catch (MediaUploadForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    private static bool TryBuildAuthorizationContext(
        ScopedMediaUploadForm request,
        int userId,
        out MediaUploadAuthorizationContext auth,
        out string? error)
    {
        auth = new MediaUploadAuthorizationContext(userId, MediaUploadScope.Post, null, null, null, null);
        error = null;

        var scopeRaw = (request.Scope ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(scopeRaw))
        {
            error = "Indique scope: \"post\" ou \"message\".";
            return false;
        }

        if (scopeRaw == "post")
        {
            auth = new MediaUploadAuthorizationContext(
                userId,
                MediaUploadScope.Post,
                request.PublicationContext,
                TryParsePositiveInt(request.CommunityId),
                null,
                null);
            return true;
        }

        if (scopeRaw == "message")
        {
            if (!int.TryParse((request.ConversationId ?? string.Empty).Trim(), out var cid) || cid <= 0)
            {
                error = "conversationId inválido para scope \"message\".";
                return false;
            }

            auth = new MediaUploadAuthorizationContext(
                userId,
                MediaUploadScope.Message,
                null,
                null,
                cid,
                null);
            return true;
        }

        error = "scope inválido. Use \"post\" ou \"message\".";
        return false;
    }

    private static int? TryParsePositiveInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return int.TryParse(raw.Trim(), out var v) && v > 0 ? v : null;
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

/// <summary>Multipart comum a imagem e vídeo (campos de contexto obrigatórios no upload).</summary>
public sealed class ScopedMediaUploadForm
{
    public IFormFile? File { get; set; }

    /// <summary><c>post</c> ou <c>message</c>.</summary>
    public string? Scope { get; set; }

    /// <summary><c>profile</c> ou <c>community</c> quando <see cref="Scope"/> é <c>post</c>.</summary>
    public string? PublicationContext { get; set; }

    public string? CommunityId { get; set; }

    public string? ConversationId { get; set; }

    /// <summary>Duração declarada (s) para vídeo; validada contra o limite do contexto.</summary>
    public int? DurationSeconds { get; set; }
}
