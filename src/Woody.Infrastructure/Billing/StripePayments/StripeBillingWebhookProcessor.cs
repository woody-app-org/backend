using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Billing.StripePayments;

public class StripeBillingWebhookProcessor : IStripeWebhookBillingProcessor
{
    private static readonly HashSet<string> HandledEventTypes = new(StringComparer.Ordinal)
    {
        "checkout.session.completed",
        "customer.subscription.created",
        "customer.subscription.updated",
        "customer.subscription.deleted",
        "invoice.paid",
        "invoice.payment_failed"
    };

    private readonly IOptions<BillingOptions> _options;
    private readonly IBillingWebhookReceiptRepository _receipts;
    private readonly IUserSubscriptionRepository _subscriptions;
    private readonly ICommunitySubscriptionRepository _communitySubscriptions;
    private readonly IBillingSubscriptionGateway _subscriptionGateway;
    private readonly WoodyDbContext _db;
    private readonly ILogger<StripeBillingWebhookProcessor> _logger;

    public StripeBillingWebhookProcessor(
        IOptions<BillingOptions> options,
        IBillingWebhookReceiptRepository receipts,
        IUserSubscriptionRepository subscriptions,
        ICommunitySubscriptionRepository communitySubscriptions,
        IBillingSubscriptionGateway subscriptionGateway,
        WoodyDbContext db,
        ILogger<StripeBillingWebhookProcessor> logger)
    {
        _options = options;
        _receipts = receipts;
        _subscriptions = subscriptions;
        _communitySubscriptions = communitySubscriptions;
        _subscriptionGateway = subscriptionGateway;
        _db = db;
        _logger = logger;
    }

    public async Task<StripeWebhookProcessOutcome> ProcessAsync(string requestBody, string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        var webhookSecret = _options.Value.Stripe?.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogWarning("Webhook Stripe rejeitado: secret não configurado.");
            return StripeWebhookProcessOutcome.NotConfigured;
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(requestBody, stripeSignatureHeader, webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Assinatura Stripe-Signature inválida.");
            return StripeWebhookProcessOutcome.InvalidSignature;
        }

        if (!HandledEventTypes.Contains(stripeEvent.Type))
            return StripeWebhookProcessOutcome.IgnoredEventType;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await _receipts.TryClaimEventAsync(stripeEvent.Id, stripeEvent.Type, cancellationToken))
                return StripeWebhookProcessOutcome.DuplicateDelivery;

            var ok = stripeEvent.Type switch
            {
                "checkout.session.completed" => await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken),
                "customer.subscription.created" => await HandleSubscriptionUpsertAsync(stripeEvent, cancellationToken),
                "customer.subscription.updated" => await HandleSubscriptionUpsertAsync(stripeEvent, cancellationToken),
                "customer.subscription.deleted" => await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken),
                "invoice.paid" => await HandleInvoicePaidAsync(stripeEvent, cancellationToken),
                "invoice.payment_failed" => await HandleInvoicePaymentFailedAsync(stripeEvent, cancellationToken),
                _ => true
            };

            if (!ok)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StripeWebhookProcessOutcome.InvalidPayload;
            }

            await _subscriptions.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return StripeWebhookProcessOutcome.Processed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar webhook Stripe {EventId} ({EventType}).", stripeEvent.Id,
                stripeEvent.Type);
            await transaction.RollbackAsync(cancellationToken);
            return StripeWebhookProcessOutcome.TransientFailure;
        }
    }

    private async Task<bool> HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not global::Stripe.Checkout.Session session)
            return false;

        if (!string.Equals(session.Mode, "subscription", StringComparison.Ordinal))
            return true;

        if (string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            _logger.LogWarning("checkout.session.completed sem subscription id.");
            return false;
        }

        var read = await _subscriptionGateway.GetSubscriptionAsync(session.SubscriptionId, cancellationToken);
        if (read is null)
        {
            _logger.LogWarning("checkout.session.completed sem leitura no gateway para {SubscriptionId}.",
                session.SubscriptionId);
            return false;
        }

        var now = DateTime.UtcNow;

        // Ramo comunidade: metadata woody_billing_subject=community_premium + woody_community_id; nunca grava em user_subscriptions.
        // Ramo utilizadora: só quando read.User está preenchido (subscrição Woody Pro). Os dois são mutuamente exclusivos em BillingSubscriptionReadResult.
        if (read.Community != null)
        {
            var communityId = TryResolveCommunityIdFromCheckoutSession(session) ?? read.WoodyCommunityIdFromStripeMetadata;
            if (communityId is null or <= 0)
            {
                _logger.LogWarning("checkout.session.completed comunidade sem id em metadata.");
                return false;
            }

            var crow = await _communitySubscriptions.GetByCommunityIdTrackedAsync(communityId.Value, cancellationToken);
            if (crow == null)
            {
                _logger.LogWarning("Comunidade {CommunityId} sem linha community_subscriptions.", communityId);
                return false;
            }

            CommunitySubscriptionStripeSync.ApplyGatewaySnapshot(crow, read.Community, now);
            _communitySubscriptions.Update(crow);
            return true;
        }

        if (read.User is null)
        {
            _logger.LogWarning("checkout.session.completed sem ramo utilizadora nem comunidade reconhecido.");
            return false;
        }

        if (!TryResolveWoodyUserId(session.ClientReferenceId, session.Metadata, out var userId))
        {
            _logger.LogWarning("checkout.session.completed sem referência de utilizadora Stripe.");
            return false;
        }

        var row = await _subscriptions.GetByUserIdTrackedAsync(userId, cancellationToken);
        if (row is null)
        {
            _logger.LogWarning("Utilizadora {UserId} sem linha de assinatura na BD.", userId);
            return false;
        }

        UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, read.User, now);
        _subscriptions.Update(row);
        return true;
    }

    private static int? TryResolveCommunityIdFromCheckoutSession(global::Stripe.Checkout.Session session)
    {
        if (session.Metadata != null &&
            session.Metadata.TryGetValue(StripeBillingMetadataKeys.WoodyCommunityId, out var raw) &&
            int.TryParse(raw, out var id))
            return id;
        return null;
    }

    private Task<bool> HandleSubscriptionUpsertAsync(Event stripeEvent, CancellationToken cancellationToken) =>
        SyncFromStripeSubscriptionObjectAsync(stripeEvent.Data.Object as global::Stripe.Subscription, cancellationToken);

    private async Task<bool> HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as global::Stripe.Subscription;
        if (subscription is null)
            return false;

        var now = DateTime.UtcNow;
        var userRow = await ResolveUserSubscriptionRowAsync(subscription, cancellationToken);
        if (userRow != null)
        {
            UserSubscriptionStripeSync.ApplySubscriptionRemoved(userRow, now);
            _subscriptions.Update(userRow);
            return true;
        }

        var commRow = await ResolveCommunitySubscriptionRowAsync(subscription, cancellationToken);
        if (commRow != null)
        {
            CommunitySubscriptionStripeSync.ApplySubscriptionRemoved(commRow, now);
            _communitySubscriptions.Update(commRow);
            return true;
        }

        return true;
    }

    private async Task<bool> HandleInvoicePaidAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as global::Stripe.Invoice;
        if (invoice is null)
            return true;

        var subscriptionId = TryGetSubscriptionIdFromInvoice(invoice);
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return true;

        return await RefreshSubscriptionFromGatewayAsync(subscriptionId, invoice.CustomerId, cancellationToken);
    }

    private async Task<bool> HandleInvoicePaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as global::Stripe.Invoice;
        if (invoice is null)
            return true;

        var subscriptionId = TryGetSubscriptionIdFromInvoice(invoice);
        if (string.IsNullOrWhiteSpace(subscriptionId))
            return true;

        return await RefreshSubscriptionFromGatewayAsync(subscriptionId, invoice.CustomerId, cancellationToken);
    }

    private static string? TryGetSubscriptionIdFromInvoice(global::Stripe.Invoice invoice)
    {
        var details = invoice.Parent?.SubscriptionDetails;
        if (details is null)
            return null;
        if (!string.IsNullOrWhiteSpace(details.SubscriptionId))
            return details.SubscriptionId;
        return details.Subscription?.Id;
    }

    private async Task<bool> SyncFromStripeSubscriptionObjectAsync(global::Stripe.Subscription? subscription,
        CancellationToken cancellationToken)
    {
        if (subscription is null)
            return false;

        var read = StripeSubscriptionStateMapper.ToReadResult(subscription, _options.Value);
        var now = DateTime.UtcNow;

        if (read.Community != null)
        {
            var row = await ResolveCommunitySubscriptionRowAsync(subscription, cancellationToken);
            if (row is null)
            {
                _logger.LogWarning("Subscrição Stripe {SubscriptionId} comunidade sem correspondência na BD.",
                    subscription.Id);
                return false;
            }

            CommunitySubscriptionStripeSync.ApplyGatewaySnapshot(row, read.Community, now);
            _communitySubscriptions.Update(row);
            return true;
        }

        if (read.User != null)
        {
            var row = await ResolveUserSubscriptionRowAsync(subscription, cancellationToken);
            if (row is null)
            {
                _logger.LogWarning("Subscrição Stripe {SubscriptionId} utilizadora sem correspondência na BD.",
                    subscription.Id);
                return false;
            }

            UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, read.User, now);
            _subscriptions.Update(row);
            return true;
        }

        return false;
    }

    private async Task<bool> RefreshSubscriptionFromGatewayAsync(string subscriptionId, string? customerId,
        CancellationToken cancellationToken)
    {
        var read = await _subscriptionGateway.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (read is null)
            return true;

        var now = DateTime.UtcNow;

        if (read.Community != null)
        {
            var row = await _communitySubscriptions.GetByProviderSubscriptionIdTrackedAsync(subscriptionId,
                cancellationToken);
            if (row is null)
                return true;

            CommunitySubscriptionStripeSync.ApplyGatewaySnapshot(row, read.Community, now);
            _communitySubscriptions.Update(row);
            return true;
        }

        if (read.User != null)
        {
            var row = await _subscriptions.GetByProviderSubscriptionIdTrackedAsync(subscriptionId, cancellationToken);
            if (row is null && !string.IsNullOrEmpty(customerId))
                row = await _subscriptions.GetByProviderCustomerIdTrackedAsync(customerId, cancellationToken);

            if (row is null)
                return true;

            UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, read.User, now);
            _subscriptions.Update(row);
            return true;
        }

        return true;
    }

    private async Task<UserSubscription?> ResolveUserSubscriptionRowAsync(global::Stripe.Subscription subscription,
        CancellationToken cancellationToken)
    {
        var row = await _subscriptions.GetByProviderSubscriptionIdTrackedAsync(subscription.Id, cancellationToken);
        if (row != null)
            return row;

        if (subscription.Metadata != null &&
            subscription.Metadata.TryGetValue(StripeBillingMetadataKeys.WoodyUserId, out var uidRaw) &&
            int.TryParse(uidRaw, out var uid))
            return await _subscriptions.GetByUserIdTrackedAsync(uid, cancellationToken);

        return null;
    }

    private async Task<CommunitySubscription?> ResolveCommunitySubscriptionRowAsync(
        global::Stripe.Subscription subscription, CancellationToken cancellationToken)
    {
        var row = await _communitySubscriptions.GetByProviderSubscriptionIdTrackedAsync(subscription.Id,
            cancellationToken);
        if (row != null)
            return row;

        if (subscription.Metadata != null &&
            subscription.Metadata.TryGetValue(StripeBillingMetadataKeys.WoodyCommunityId, out var raw) &&
            int.TryParse(raw, out var cid))
            return await _communitySubscriptions.GetByCommunityIdTrackedAsync(cid, cancellationToken);

        return null;
    }

    private static bool TryResolveWoodyUserId(string? clientReferenceId, IDictionary<string, string>? metadata,
        out int userId)
    {
        userId = 0;
        if (!string.IsNullOrWhiteSpace(clientReferenceId) && int.TryParse(clientReferenceId, out userId))
            return true;

        if (metadata != null &&
            metadata.TryGetValue(StripeBillingMetadataKeys.WoodyUserId, out var m) &&
            int.TryParse(m, out userId))
            return true;

        return false;
    }
}
