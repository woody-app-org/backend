using Microsoft.Extensions.Options;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Billing;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;

namespace Woody.Application.UseCases.Billing;

public class CreateCustomerPortalSessionHandler
{
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IBillingCustomerPortalGateway _portalGateway;
    private readonly BillingOptions _billingOptions;

    public CreateCustomerPortalSessionHandler(
        IUserSubscriptionRepository subscriptions,
        IBillingCustomerPortalGateway portalGateway,
        IOptions<BillingOptions> billingOptions)
    {
        _subscriptions = subscriptions;
        _portalGateway = portalGateway;
        _billingOptions = billingOptions.Value;
    }

    public async Task<BillingPortalSessionResponseDto> HandleAsync(int userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_billingOptions.Stripe?.SecretKey))
            throw new InvalidOperationException("Chave Stripe não configurada.");

        var returnUrl = _billingOptions.Stripe.CustomerPortalReturnUrl?.Trim();
        if (string.IsNullOrEmpty(returnUrl))
            throw new InvalidOperationException("URL de regresso do portal Stripe não configurada.");

        var subscription = await _subscriptions.GetByUserIdTrackedAsync(userId, cancellationToken);
        if (subscription == null)
            throw new KeyNotFoundException("Estado de assinatura em falta; contacta o suporte.");

        var customerId = subscription.ProviderCustomerId?.Trim();
        if (string.IsNullOrEmpty(customerId))
            throw new InvalidOperationException(
                "Não há cliente Stripe associado. Inicia uma subscrição na Woody para abrir a área de gestão na Stripe.");

        var configurationId = _billingOptions.Stripe.CustomerPortalConfigurationId?.Trim();
        var gatewayResult = await _portalGateway.CreateSessionAsync(
            new BillingCustomerPortalSessionRequest(customerId, returnUrl,
                string.IsNullOrEmpty(configurationId) ? null : configurationId),
            cancellationToken);

        if (!gatewayResult.Ok || string.IsNullOrEmpty(gatewayResult.Url))
            throw new InvalidOperationException(gatewayResult.ErrorMessage ?? "Não foi possível abrir o portal Stripe.");

        return new BillingPortalSessionResponseDto { Url = gatewayResult.Url };
    }
}
