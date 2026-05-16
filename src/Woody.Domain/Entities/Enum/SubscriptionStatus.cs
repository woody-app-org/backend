namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Ciclo de vida da assinatura paga; para plano <see cref="SubscriptionPlan.Free"/> usar <see cref="Active"/>.
/// </summary>
public enum SubscriptionStatus
{
    Active = 0,
    Trialing = 1,
    PastDue = 2,
    Canceling = 3,
    Canceled = 4,
    Expired = 5
}
