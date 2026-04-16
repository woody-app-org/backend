using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

/// <summary>
/// Porta de entrada para feature gating no backend (handlers/controllers chamam aqui em vez de duplicar regras).
/// </summary>
public interface IUserEntitlementService
{
    Task<UserSubscription?> GetSubscriptionAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveProBenefitsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> CanCreateCommunityAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessPremiumFeatureAsync(int userId, CancellationToken cancellationToken = default);
}
