using Microsoft.Extensions.Options;
using Stripe;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces.Billing;

namespace Woody.Infrastructure.Billing.StripePayments;

public class StripeBillingWebhookSignatureVerifier : IBillingWebhookSignatureVerifier
{
    private readonly IOptions<BillingOptions> _options;

    public StripeBillingWebhookSignatureVerifier(IOptions<BillingOptions> options)
    {
        _options = options;
    }

    public BillingWebhookValidationResult Validate(string requestBody, string stripeSignatureHeader)
    {
        var webhookSecret = _options.Value.Stripe?.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return new BillingWebhookValidationResult(false, "Billing:Stripe:WebhookSecret não configurado.", null,
                null, null);
        }

        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            return new BillingWebhookValidationResult(false, "Cabeçalho Stripe-Signature ausente.", null, null, null);
        }

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(requestBody, stripeSignatureHeader, webhookSecret,
                throwOnApiVersionMismatch: false);
            return new BillingWebhookValidationResult(true, null, stripeEvent.Id, stripeEvent.Type, requestBody);
        }
        catch (StripeException ex)
        {
            return new BillingWebhookValidationResult(false, ex.Message, null, null, null);
        }
    }
}
