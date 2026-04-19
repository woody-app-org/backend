using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Interfaces.Billing;
using Woody.Domain.Entities;

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
    private readonly IBillingSubscriptionGateway _subscriptionGateway;
    private readonly ILogger<StripeBillingWebhookProcessor> _logger;

    public StripeBillingWebhookProcessor(
        IOptions<BillingOptions> options,
        IBillingWebhookReceiptRepository receipts,
        IUserSubscriptionRepository subscriptions,
        IBillingSubscriptionGateway subscriptionGateway,
        ILogger<StripeBillingWebhookProcessor> logger)
    {
        _options = options;
        _receipts = receipts;
        _subscriptions = subscriptions;
        _subscriptionGateway = subscriptionGateway;
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

        if (!await _receipts.TryClaimEventAsync(stripeEvent.Id, stripeEvent.Type, cancellationToken))
            return StripeWebhookProcessOutcome.DuplicateDelivery;

        try
        {
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
                await _receipts.ReleaseClaimAsync(stripeEvent.Id, cancellationToken);
                return StripeWebhookProcessOutcome.InvalidPayload;
            }

            await _subscriptions.SaveChangesAsync(cancellationToken);
            return StripeWebhookProcessOutcome.Processed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar webhook Stripe {EventId} ({EventType}).", stripeEvent.Id,
                stripeEvent.Type);
            await _receipts.ReleaseClaimAsync(stripeEvent.Id, cancellationToken);
            return StripeWebhookProcessOutcome.TransientFailure;
        }
    }

    private async Task<bool> HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not global::Stripe.Checkout.Session session)
            return false;

        if (!string.Equals(session.Mode, "subscription", StringComparison.Ordinal))
            return true;

        if (!TryResolveWoodyUserId(session.ClientReferenceId, session.Metadata, out var userId))
        {
            _logger.LogWarning("checkout.session.completed sem referência de utilizadora Stripe.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            _logger.LogWarning("checkout.session.completed sem subscription id.");
            return false;
        }

        var snapshot = await _subscriptionGateway.GetSubscriptionAsync(session.SubscriptionId, cancellationToken);
        if (snapshot is null)
        {
            _logger.LogWarning("Não foi possível obter a subscrição Stripe {SubscriptionId} após checkout.",
                session.SubscriptionId);
            return false;
        }

        var row = await _subscriptions.GetByUserIdTrackedAsync(userId, cancellationToken);
        if (row is null)
        {
            _logger.LogWarning("Utilizadora {UserId} sem linha de assinatura na BD.", userId);
            return false;
        }

        UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, snapshot, DateTime.UtcNow);
        _subscriptions.Update(row);
        return true;
    }

    private Task<bool> HandleSubscriptionUpsertAsync(Event stripeEvent, CancellationToken cancellationToken) =>
        SyncFromStripeSubscriptionObjectAsync(stripeEvent.Data.Object as global::Stripe.Subscription, cancellationToken);

    private async Task<bool> HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as global::Stripe.Subscription;
        if (subscription is null)
            return false;

        var row = await ResolveSubscriptionRowAsync(subscription, cancellationToken);
        if (row is null)
            return true;

        UserSubscriptionStripeSync.ApplySubscriptionRemoved(row, DateTime.UtcNow);
        _subscriptions.Update(row);
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

        var snapshot = StripeSubscriptionStateMapper.ToSnapshot(subscription, _options.Value);
        var row = await ResolveSubscriptionRowAsync(subscription, cancellationToken);
        if (row is null)
        {
            _logger.LogWarning("Subscrição Stripe {SubscriptionId} sem correspondência na BD.", subscription.Id);
            return false;
        }

        UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, snapshot, DateTime.UtcNow);
        _subscriptions.Update(row);
        return true;
    }

    private async Task<bool> RefreshSubscriptionFromGatewayAsync(string subscriptionId, string? customerId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _subscriptionGateway.GetSubscriptionAsync(subscriptionId, cancellationToken);
        if (snapshot is null)
            return true;

        var row = await _subscriptions.GetByProviderSubscriptionIdTrackedAsync(subscriptionId, cancellationToken);
        if (row is null && !string.IsNullOrEmpty(customerId))
            row = await _subscriptions.GetByProviderCustomerIdTrackedAsync(customerId, cancellationToken);

        if (row is null)
            return true;

        UserSubscriptionStripeSync.ApplyGatewaySnapshot(row, snapshot, DateTime.UtcNow);
        _subscriptions.Update(row);
        return true;
    }

    private async Task<UserSubscription?> ResolveSubscriptionRowAsync(global::Stripe.Subscription subscription,
        CancellationToken cancellationToken)
    {
        var row = await _subscriptions.GetByProviderSubscriptionIdTrackedAsync(subscription.Id, cancellationToken);
        if (row != null)
            return row;

        if (subscription.Metadata != null &&
            subscription.Metadata.TryGetValue("woody_user_id", out var uidRaw) &&
            int.TryParse(uidRaw, out var uid))
            return await _subscriptions.GetByUserIdTrackedAsync(uid, cancellationToken);

        return null;
    }

    private static bool TryResolveWoodyUserId(string? clientReferenceId, IDictionary<string, string>? metadata,
        out int userId)
    {
        userId = 0;
        if (!string.IsNullOrWhiteSpace(clientReferenceId) && int.TryParse(clientReferenceId, out userId))
            return true;

        if (metadata != null && metadata.TryGetValue("woody_user_id", out var m) && int.TryParse(m, out userId))
            return true;

        return false;
    }
}
