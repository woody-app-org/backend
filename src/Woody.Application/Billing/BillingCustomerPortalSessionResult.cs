namespace Woody.Application.Billing;

public sealed record BillingCustomerPortalSessionResult(bool Ok, string? Url, string? ErrorMessage);
