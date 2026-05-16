using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using Woody.Application.Billing;
using Woody.Application.Configuration;
using Woody.Application.Interfaces.Billing;

namespace Woody.Infrastructure.Billing.StripePayments;

public class StripeBillingCheckoutGateway : IBillingCheckoutGateway
{
    private readonly IOptions<BillingOptions> _options;

    public StripeBillingCheckoutGateway(IOptions<BillingOptions> options)
    {
        _options = options;
    }

    public async Task<BillingCheckoutSessionResult> CreateSubscriptionCheckoutAsync(
        BillingCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var secretKey = _options.Value.Stripe?.SecretKey;
        if (string.IsNullOrWhiteSpace(secretKey))
            return new BillingCheckoutSessionResult(false, null, "Chave Stripe não configurada.", null, null);

        var client = new global::Stripe.StripeClient(secretKey);
        var customerService = new global::Stripe.CustomerService(client);
        var sessionService = new global::Stripe.Checkout.SessionService(client);

        string customerId;
        try
        {
            if (!string.IsNullOrWhiteSpace(request.ExistingStripeCustomerId))
            {
                customerId = request.ExistingStripeCustomerId;
            }
            else
            {
                var created = await customerService.CreateAsync(
                    new global::Stripe.CustomerCreateOptions
                    {
                        Email = request.Email,
                        Metadata = new Dictionary<string, string>
                        {
                            ["woody_user_id"] = request.UserId.ToString()
                        }
                    },
                    cancellationToken: cancellationToken);
                customerId = created.Id;
            }

            var subject = request.SubjectKind == BillingCheckoutSubjectKind.CommunityPremium
                ? StripeBillingMetadataKeys.SubjectCommunityPremium
                : StripeBillingMetadataKeys.SubjectUserPro;

            var meta = new Dictionary<string, string>
            {
                [StripeBillingMetadataKeys.WoodyUserId] = request.UserId.ToString(),
                [StripeBillingMetadataKeys.PlanCode] = request.PlanCode,
                [StripeBillingMetadataKeys.WoodyBillingSubject] = subject
            };

            if (request.CommunityId is > 0)
                meta[StripeBillingMetadataKeys.WoodyCommunityId] = request.CommunityId.Value.ToString();

            var session = await sessionService.CreateAsync(
                new SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = customerId,
                    ClientReferenceId = request.UserId.ToString(),
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new()
                        {
                            Price = request.StripePriceId,
                            Quantity = 1
                        }
                    },
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = meta,
                    SubscriptionData = new SessionSubscriptionDataOptions { Metadata = meta },
                    AllowPromotionCodes = false
                },
                requestOptions: new global::Stripe.RequestOptions
                {
                    IdempotencyKey = request.IdempotencyKey
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(session.Url))
                return new BillingCheckoutSessionResult(false, null, "Resposta Stripe sem URL de checkout.", customerId, session.Id);

            return new BillingCheckoutSessionResult(true, session.Url, null, customerId, session.Id);
        }
        catch (global::Stripe.StripeException ex)
        {
            return new BillingCheckoutSessionResult(false, null, ex.Message, null, null);
        }
    }
}
