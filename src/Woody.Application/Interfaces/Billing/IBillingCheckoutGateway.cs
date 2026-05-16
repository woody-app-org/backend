using Woody.Application.Billing;

namespace Woody.Application.Interfaces.Billing;

/// <summary>Criação de sessão Stripe Checkout (modo subscription).</summary>
public interface IBillingCheckoutGateway
{
    Task<BillingCheckoutSessionResult> CreateSubscriptionCheckoutAsync(BillingCheckoutSessionRequest request,
        CancellationToken cancellationToken = default);
}
