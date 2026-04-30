using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces.Messaging;

namespace Woody.Api.Controllers;

/// <summary>Pesquisa plugável de GIF/stickers para o chat (não acopla a GIPHY/Tenor na API pública).</summary>
[ApiController]
[Authorize]
[Route("api/messaging")]
[EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
public sealed class MessagingStickerGifSearchController : ControllerBase
{
    private readonly IGifStickerSearchProvider _provider;

    public MessagingStickerGifSearchController(IGifStickerSearchProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Lista GIF/stickers do provedor configurado (ex.: catálogo local). Query opcional filtra por palavras.</summary>
    [HttpGet("sticker-gifs")]
    [ProducesResponseType(typeof(GifStickerSearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GifStickerSearchResponseDto>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 24,
        CancellationToken cancellationToken = default)
    {
        var me = User.GetUserId();
        if (me == null)
            return Unauthorized();

        var result = await _provider.SearchAsync(q, limit, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
