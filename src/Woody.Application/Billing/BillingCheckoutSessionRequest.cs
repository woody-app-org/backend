namespace Woody.Application.Billing;

public sealed record BillingCheckoutSessionRequest(
    int UserId,
    string Email,
    string? ExistingStripeCustomerId,
    string StripePriceId,
    string PlanCode,
    string SuccessUrl,
    string CancelUrl);
