namespace Woody.Application.Billing;

public sealed record BillingCheckoutSessionResult(bool Ok, string? Url, string? ErrorMessage, string? StripeCustomerId);
