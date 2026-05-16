namespace Woody.Application.Billing;

public sealed record BillingCustomerPortalSessionRequest(
    string StripeCustomerId,
    string ReturnUrl,
    string? ConfigurationId);
