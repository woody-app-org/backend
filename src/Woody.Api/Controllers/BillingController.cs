using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Woody.Api.Extensions;
using Woody.Application.DTOs.Billing;
using Woody.Application.UseCases.Billing;

namespace Woody.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly CreateCheckoutSessionHandler _createCheckoutSession;

    public BillingController(CreateCheckoutSessionHandler createCheckoutSession)
    {
        _createCheckoutSession = createCheckoutSession;
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
}
