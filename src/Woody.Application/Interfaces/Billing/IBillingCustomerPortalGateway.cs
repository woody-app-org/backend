using Woody.Application.Billing;

namespace Woody.Application.Interfaces.Billing;

/// <summary>Sessão do Stripe Customer Billing Portal (gestão de método de pagamento, cancelamento, faturas).</summary>
public interface IBillingCustomerPortalGateway
{
    Task<BillingCustomerPortalSessionResult> CreateSessionAsync(BillingCustomerPortalSessionRequest request,
        CancellationToken cancellationToken = default);
}
