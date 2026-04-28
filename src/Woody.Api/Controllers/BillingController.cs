using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Billing;
using Woody.Application.UseCases.Billing;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicyNames.AuthenticatedApi)]
public class BillingController : ControllerBase
{
    private readonly CreateCheckoutSessionHandler _createCheckoutSession;
    private readonly CreateCommunityPremiumCheckoutSessionHandler _createCommunityPremiumCheckoutSession;
    private readonly CreateCustomerPortalSessionHandler _createCustomerPortalSession;

    public BillingController(
        CreateCheckoutSessionHandler createCheckoutSession,
        CreateCommunityPremiumCheckoutSessionHandler createCommunityPremiumCheckoutSession,
        CreateCustomerPortalSessionHandler createCustomerPortalSession)
    {
        _createCheckoutSession = createCheckoutSession;
        _createCommunityPremiumCheckoutSession = createCommunityPremiumCheckoutSession;
        _createCustomerPortalSession = createCustomerPortalSession;
    }

    /// <summary>Inicia checkout Stripe (subscription). O <c>planCode</c> é validado no servidor.</summary>
    [HttpPost("checkout/subscription")]
    public async Task<ActionResult<CreateBillingCheckoutResponseDto>> CreateSubscriptionCheckout(
        [FromBody] CreateBillingCheckoutRequestDto body,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _createCheckoutSession.HandleAsync(userId.Value, body, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("Já tens Woody Pro", StringComparison.Ordinal))
                return Conflict(new { error = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Inicia checkout Stripe para plano premium da comunidade (preço e plano definidos no servidor).</summary>
    [HttpPost("checkout/community-premium")]
    public async Task<ActionResult<CreateBillingCheckoutResponseDto>> CreateCommunityPremiumCheckout(
        [FromBody] CreateCommunityPremiumCheckoutRequestDto body,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _createCommunityPremiumCheckoutSession.HandleAsync(userId.Value, body, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("já tem plano premium", StringComparison.OrdinalIgnoreCase))
                return Conflict(new { error = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>Abre sessão do Stripe Customer Billing Portal (cartão, faturas, cancelamento/reversão).</summary>
    [HttpPost("portal/session")]
    public async Task<ActionResult<BillingPortalSessionResponseDto>> CreateBillingPortalSession(
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _createCustomerPortalSession.HandleAsync(userId.Value, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
