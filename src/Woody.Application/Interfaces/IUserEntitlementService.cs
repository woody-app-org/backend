using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

/// <summary>
/// Porta de entrada para feature gating por <strong>plano / assinatura</strong> (ex.: criar comunidade).
/// Moderação dentro de uma comunidade (owner/admin) usa <see cref="ICommunityPermissionService"/> — não misturar os dois conceitos.
/// </summary>
public interface IUserEntitlementService
{
    Task<UserSubscription?> GetSubscriptionAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveProBenefitsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> CanCreateCommunityAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessPremiumFeatureAsync(int userId, CancellationToken cancellationToken = default);
}
