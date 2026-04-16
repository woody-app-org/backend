using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Assinatura/plano da utilizadora (1:1 com <see cref="User"/>). Fonte de verdade para benefícios Pro e futura integração com gateway.
/// </summary>
public class UserSubscription
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }

    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }

    public string? ExternalCustomerId { get; set; }
    public string? ExternalSubscriptionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
