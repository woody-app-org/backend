namespace Woody.Application.Billing;

/// <summary>Códigos de catálogo persistidos em <c>plan_code</c> (independentes do enum <see cref="Woody.Domain.Entities.Enum.SubscriptionPlan"/>).</summary>
public static class BillingPlanCodes
{
    public const string Free = "free";
    public const string ProMonthly = "pro_monthly";
}
