namespace Woody.Application.Billing;

/// <summary>Códigos de catálogo persistidos em <c>plan_code</c> (independentes do enum <see cref="Woody.Domain.Entities.Enum.SubscriptionPlan"/>).</summary>
public static class BillingPlanCodes
{
    public const string Free = "free";
    public const string ProMonthly = "pro_monthly";
    public const string ProAnnual = "pro_annual";
    public const string MaxMonthly = "max_monthly";
    public const string MaxAnnual = "max_annual";
}
