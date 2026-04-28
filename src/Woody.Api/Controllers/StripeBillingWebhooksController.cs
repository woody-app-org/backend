using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Woody.Api.Configuration;
using Woody.Application.Interfaces.Billing;

namespace Woody.Api.Controllers;

/// <summary>Webhook Stripe (sem JWT). Corpo bruto necessário para validar <c>Stripe-Signature</c>.</summary>
[ApiController]
[Route("api/billing")]
public class StripeBillingWebhooksController : ControllerBase
{
    [HttpPost("webhook")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.StripeWebhook)]
    public async Task<IActionResult> Post(
        [FromServices] IStripeWebhookBillingProcessor processor,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        var signature = Request.Headers["Stripe-Signature"].ToString();
        var outcome = await processor.ProcessAsync(payload, signature, cancellationToken);

        return outcome switch
        {
            StripeWebhookProcessOutcome.Processed
                or StripeWebhookProcessOutcome.DuplicateDelivery
                or StripeWebhookProcessOutcome.IgnoredEventType
                or StripeWebhookProcessOutcome.InvalidPayload => Ok(),
            StripeWebhookProcessOutcome.InvalidSignature => BadRequest(),
            StripeWebhookProcessOutcome.NotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable),
            StripeWebhookProcessOutcome.TransientFailure => StatusCode(StatusCodes.Status500InternalServerError),
            _ => Ok()
        };
    }
}
