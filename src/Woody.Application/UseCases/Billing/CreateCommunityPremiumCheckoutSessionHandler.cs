using Microsoft.Extensions.Options;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.DTOs.Billing;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Subscription;
using CheckoutAttemptSubjectKind = Woody.Domain.Entities.Enum.BillingCheckoutAttemptSubjectKind;

namespace Woody.Application.UseCases.Billing;

public class CreateCommunityPremiumCheckoutSessionHandler
{
    private readonly IUserRepository _users;
    private readonly IUserSubscriptionRepository _userSubscriptions;
    private readonly ICommunitySubscriptionRepository _communitySubscriptions;
    private readonly ICommunityRepository _communities;
    private readonly ICommunityPermissionService _communityPermissions;
    private readonly IBillingCheckoutAttemptRepository _checkoutAttempts;
    private readonly IBillingCheckoutGateway _checkoutGateway;
    private readonly BillingOptions _billingOptions;

    public CreateCommunityPremiumCheckoutSessionHandler(
        IUserRepository users,
        IUserSubscriptionRepository userSubscriptions,
        ICommunitySubscriptionRepository communitySubscriptions,
        ICommunityRepository communities,
        ICommunityPermissionService communityPermissions,
        IBillingCheckoutAttemptRepository checkoutAttempts,
        IBillingCheckoutGateway checkoutGateway,
        IOptions<BillingOptions> billingOptions)
    {
        _users = users;
        _userSubscriptions = userSubscriptions;
        _communitySubscriptions = communitySubscriptions;
        _communities = communities;
        _communityPermissions = communityPermissions;
        _checkoutAttempts = checkoutAttempts;
        _checkoutGateway = checkoutGateway;
        _billingOptions = billingOptions.Value;
    }

    public async Task<CreateBillingCheckoutResponseDto> HandleAsync(
        int userId,
        CreateCommunityPremiumCheckoutRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.CommunityId <= 0)
            throw new ArgumentException("Comunidade inválida.");

        if (!await _communities.ExistsNoTrackingAsync(request.CommunityId, cancellationToken))
            throw new KeyNotFoundException("Comunidade não encontrada.");

        if (!await _communityPermissions.CanModerateCommunityAsync(request.CommunityId, userId, cancellationToken))
            throw new UnauthorizedAccessException("Apenas owner ou admin da comunidade pode subscrever o plano premium.");

        if (string.IsNullOrWhiteSpace(_billingOptions.Stripe?.SecretKey))
            throw new InvalidOperationException("Chave Stripe não configurada.");

        if (string.IsNullOrWhiteSpace(_billingOptions.Stripe.CheckoutSuccessUrl) ||
            string.IsNullOrWhiteSpace(_billingOptions.Stripe.CheckoutCancelUrl))
            throw new InvalidOperationException("URLs de retorno do checkout Stripe não configuradas.");

        var planCode = CommunityBillingPlanCodes.PremiumMonthly;
        if (!BillingPlanCatalog.TryResolveCommunityStripePriceId(_billingOptions, planCode, out var priceId))
            throw new InvalidOperationException("Billing Stripe não está configurado para o plano premium da comunidade.");

        var user = await _users.GetByIdTrackedAsync(userId, cancellationToken)
                   ?? throw new KeyNotFoundException("Utilizadora não encontrada.");

        var userSub = await _userSubscriptions.GetByUserIdTrackedAsync(userId, cancellationToken)
                      ?? throw new InvalidOperationException("Estado de assinatura em falta; contacta o suporte.");

        var communitySub =
            await _communitySubscriptions.GetByCommunityIdTrackedAsync(request.CommunityId, cancellationToken)
            ?? throw new InvalidOperationException("Estado de assinatura da comunidade em falta; contacta o suporte.");

        var utcNow = DateTime.UtcNow;
        if (CommunitySubscriptionEntitlement.HasActivePremiumBenefits(communitySub, utcNow))
            throw new InvalidOperationException("Esta comunidade já tem plano premium ativo.");

        var existingCustomer =
            userSub.BillingProvider == BillingProvider.Stripe ? userSub.ProviderCustomerId : null;

        var idempotencyKey = BuildCheckoutIdempotencyKey(
            user.Id,
            CheckoutAttemptSubjectKind.CommunityPremium,
            planCode,
            request.CommunityId);
        var existingAttempt = await _checkoutAttempts.GetReusableAsync(idempotencyKey, utcNow, cancellationToken);
        if (!string.IsNullOrWhiteSpace(existingAttempt?.StripeSessionUrl))
            return new CreateBillingCheckoutResponseDto { Url = existingAttempt.StripeSessionUrl };

        var attempt = await _checkoutAttempts.ClaimOrGetAsync(
            idempotencyKey,
            user.Id,
            CheckoutAttemptSubjectKind.CommunityPremium,
            planCode,
            request.CommunityId,
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
                _billingOptions.Stripe.CheckoutCancelUrl.Trim(),
                BillingCheckoutSubjectKind.CommunityPremium,
                request.CommunityId),
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
            !string.Equals(userSub.ProviderCustomerId, gatewayResult.StripeCustomerId, StringComparison.Ordinal))
        {
            userSub.ProviderCustomerId = gatewayResult.StripeCustomerId;
            userSub.BillingProvider = BillingProvider.Stripe;
            userSub.UpdatedAt = utcNow;
            _userSubscriptions.Update(userSub);
            await _userSubscriptions.SaveChangesAsync(cancellationToken);
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
