using Microsoft.Extensions.Options;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Billing;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Application.Validation;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Subscription;
using CheckoutAttemptSubjectKind = Woody.Domain.Entities.Enum.BillingCheckoutAttemptSubjectKind;

namespace Woody.Application.UseCases.Billing;

public class CreateCheckoutSessionHandler
{
    private readonly IUserRepository _users;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly IBillingCheckoutAttemptRepository _checkoutAttempts;
    private readonly IBillingCheckoutGateway _checkoutGateway;
    private readonly BillingOptions _billingOptions;

    public CreateCheckoutSessionHandler(
        IUserRepository users,
        IUserSubscriptionRepository subscriptions,
        IBillingCheckoutAttemptRepository checkoutAttempts,
        IBillingCheckoutGateway checkoutGateway,
        IOptions<BillingOptions> billingOptions)
    {
        _users = users;
        _subscriptions = subscriptions;
        _checkoutAttempts = checkoutAttempts;
        _checkoutGateway = checkoutGateway;
        _billingOptions = billingOptions.Value;
    }

    public async Task<CreateBillingCheckoutResponseDto> HandleAsync(
        int userId,
        CreateBillingCheckoutRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var planCode = request.PlanCode?.Trim() ?? string.Empty;
        if (planCode.Length > InputValidationLimits.PlanCodeMaxLength)
            throw new ArgumentException("Plano inválido ou não disponível para checkout.");

        if (!BillingPlanCatalog.IsKnownCheckoutPlanCode(planCode))
            throw new ArgumentException("Plano inválido ou não disponível para checkout.");

        if (!BillingPlanCatalog.TryResolveStripePriceId(_billingOptions, planCode, out var priceId))
            throw new InvalidOperationException("Billing Stripe não está configurado para este plano.");

        if (string.IsNullOrWhiteSpace(_billingOptions.Stripe?.SecretKey))
            throw new InvalidOperationException("Chave Stripe não configurada.");

        if (string.IsNullOrWhiteSpace(_billingOptions.Stripe.CheckoutSuccessUrl) ||
            string.IsNullOrWhiteSpace(_billingOptions.Stripe.CheckoutCancelUrl))
            throw new InvalidOperationException("URLs de retorno do checkout Stripe não configuradas.");

        var user = await _users.GetByIdTrackedAsync(userId, cancellationToken)
                   ?? throw new KeyNotFoundException("Utilizadora não encontrada.");

        var subscription = await _subscriptions.GetByUserIdTrackedAsync(userId, cancellationToken);
        if (subscription == null)
            throw new InvalidOperationException("Estado de assinatura em falta; contacta o suporte.");

        var utcNow = DateTime.UtcNow;
        if (SubscriptionEntitlement.HasActiveProBenefits(subscription, utcNow))
            throw new InvalidOperationException("Já tens Woody Pro ativo; não é necessário voltar a subscrever.");

        var existingCustomer =
            subscription.BillingProvider == BillingProvider.Stripe ? subscription.ProviderCustomerId : null;

        var idempotencyKey = BuildCheckoutIdempotencyKey(
            user.Id,
            CheckoutAttemptSubjectKind.UserSubscription,
            planCode,
            communityId: null);
        var existingAttempt = await _checkoutAttempts.GetReusableAsync(idempotencyKey, utcNow, cancellationToken);
        if (!string.IsNullOrWhiteSpace(existingAttempt?.StripeSessionUrl))
            return new CreateBillingCheckoutResponseDto { Url = existingAttempt.StripeSessionUrl };

        var attempt = await _checkoutAttempts.ClaimOrGetAsync(
            idempotencyKey,
            user.Id,
            CheckoutAttemptSubjectKind.UserSubscription,
            planCode,
            communityId: null,
            utcNow,
            utcNow.AddHours(24),
            cancellationToken);

        var gatewayResult = await _checkoutGateway.CreateSubscriptionCheckoutAsync(
            new BillingCheckoutSessionRequest(
                user.Id,
                user.Email,
                existingCustomer,
                idempotencyKey,
                priceId,
                planCode,
                _billingOptions.Stripe.CheckoutSuccessUrl.Trim(),
                _billingOptions.Stripe.CheckoutCancelUrl.Trim()),
            cancellationToken);

        if (!gatewayResult.Ok || string.IsNullOrEmpty(gatewayResult.Url) || string.IsNullOrEmpty(gatewayResult.StripeSessionId))
        {
            _checkoutAttempts.MarkFailed(attempt, DateTime.UtcNow);
            await _checkoutAttempts.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException(gatewayResult.ErrorMessage ?? "Não foi possível iniciar o checkout.");
        }

        _checkoutAttempts.MarkSessionCreated(
            attempt,
            gatewayResult.StripeSessionId,
            gatewayResult.Url,
            gatewayResult.StripeCustomerId,
            DateTime.UtcNow);
        await _checkoutAttempts.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(gatewayResult.StripeCustomerId) &&
            !string.Equals(subscription.ProviderCustomerId, gatewayResult.StripeCustomerId, StringComparison.Ordinal))
        {
            subscription.ProviderCustomerId = gatewayResult.StripeCustomerId;
            subscription.BillingProvider = BillingProvider.Stripe;
            subscription.UpdatedAt = DateTime.UtcNow;
            _subscriptions.Update(subscription);
            await _subscriptions.SaveChangesAsync(cancellationToken);
        }

        return new CreateBillingCheckoutResponseDto { Url = gatewayResult.Url };
    }

    private static string BuildCheckoutIdempotencyKey(
        int userId,
        CheckoutAttemptSubjectKind subjectKind,
        string planCode,
        int? communityId) =>
        $"woody-checkout:v1:user:{userId}:subject:{subjectKind}:plan:{planCode}:community:{communityId?.ToString() ?? "none"}";
}
