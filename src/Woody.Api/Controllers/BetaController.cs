using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Application.Beta;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;

namespace Woody.Api.Controllers;

[ApiController]
[Route("api/beta")]
public class BetaController : ControllerBase
{
    private readonly IBetaInviteRepository _betaInvites;

    public BetaController(IBetaInviteRepository betaInvites)
    {
        _betaInvites = betaInvites;
    }

    /// <summary>Validação prévia do convite (sem autenticação). Não revele detalhes sobre o motivo da falha.</summary>
    [HttpPost("validate-invite")]
    [EnableRateLimiting(RateLimitPolicyNames.BetaInviteValidate)]
    public async Task<ActionResult<ValidateInviteResponseDTO>> ValidateInvite(
        [FromBody] ValidateInviteRequestDTO request,
        CancellationToken cancellationToken)
    {
        var normalized = BetaInviteNormalizer.Normalize(request?.Code);
        if (string.IsNullOrEmpty(normalized))
        {
            return Ok(new ValidateInviteResponseDTO
            {
                Valid = false,
                Message = BetaInviteMessages.PublicInvalid
            });
        }

        var valid = await _betaInvites.IsValidForPreviewAsync(normalized, cancellationToken);
        if (!valid)
        {
            return Ok(new ValidateInviteResponseDTO
            {
                Valid = false,
                Message = BetaInviteMessages.PublicInvalid
            });
        }

        return Ok(new ValidateInviteResponseDTO { Valid = true });
    }
}
