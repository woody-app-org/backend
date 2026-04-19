using Microsoft.Extensions.Options;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces.Billing;

namespace Woody.Infrastructure.Billing.StripePayments;

public class StripeBillingCustomerPortalGateway : IBillingCustomerPortalGateway
{
    private readonly IOptions<BillingOptions> _options;

    public StripeBillingCustomerPortalGateway(IOptions<BillingOptions> options)
    {
        _options = options;
    }

    public async Task<BillingCustomerPortalSessionResult> CreateSessionAsync(
        BillingCustomerPortalSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var secretKey = _options.Value.Stripe?.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            return new BillingCustomerPortalSessionResult(false, null, "Chave Stripe não configurada.");

        var client = new global::Stripe.StripeClient(secretKey);
        var service = new global::Stripe.BillingPortal.SessionService(client);

        try
        {
            var options = new global::Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = request.StripeCustomerId,
                ReturnUrl = request.ReturnUrl
            };
            if (!string.IsNullOrWhiteSpace(request.ConfigurationId))
                options.Configuration = request.ConfigurationId;

            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);
            if (string.IsNullOrEmpty(session.Url))
                return new BillingCustomerPortalSessionResult(false, null, "Resposta Stripe sem URL do portal.");

            return new BillingCustomerPortalSessionResult(true, session.Url, null);
        }
        catch (global::Stripe.StripeException ex)
        {
            return new BillingCustomerPortalSessionResult(false, null, ex.Message);
        }
    }
}
