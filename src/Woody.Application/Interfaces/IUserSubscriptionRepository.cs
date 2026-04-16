using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

/// <summary>
/// Persistência 1:1 de <see cref="UserSubscription"/>. Pontos de extensão típicos com pagamento:
/// <c>GetByUserIdTrackedAsync</c> + <see cref="Update"/> após webhooks (invoice paid, canceled, etc.);
/// <see cref="UserSubscription.ExternalCustomerId"/> / <see cref="UserSubscription.ExternalSubscriptionId"/> para correlacionar com o gateway.
/// </summary>
public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByUserIdNoTrackingAsync(int userId, CancellationToken cancellationToken = default);
    Task<UserSubscription?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);
    void Update(UserSubscription subscription);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
