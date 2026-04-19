using Microsoft.Extensions.Options;
using Stripe;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces.Billing;

namespace Woody.Infrastructure.Billing.StripePayments;

public class StripeBillingSubscriptionGateway : IBillingSubscriptionGateway
{
    private readonly IOptions<BillingOptions> _options;

    public StripeBillingSubscriptionGateway(IOptions<BillingOptions> options)
    {
        _options = options;
    }

    public async Task<BillingSubscriptionSnapshot?> GetSubscriptionAsync(string providerSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var secretKey = _options.Value.Stripe?.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            return null;

        var client = new StripeClient(secretKey);
        var service = new SubscriptionService(client);

        try
        {
            var subscription = await service.GetAsync(providerSubscriptionId, cancellationToken: cancellationToken);
            return StripeSubscriptionStateMapper.ToSnapshot(subscription, _options.Value);
        }
        catch (global::Stripe.StripeException)
        {
            return null;
        }
    }
}
