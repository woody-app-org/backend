using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class CommunitySubscriptionRepository : ICommunitySubscriptionRepository
{
    private readonly WoodyDbContext _context;

    public CommunitySubscriptionRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<CommunitySubscription?> GetByCommunityIdNoTrackingAsync(int communityId,
        CancellationToken cancellationToken = default) =>
        _context.CommunitySubscriptions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CommunityId == communityId, cancellationToken);

    public Task<CommunitySubscription?> GetByCommunityIdTrackedAsync(int communityId,
        CancellationToken cancellationToken = default) =>
        _context.CommunitySubscriptions.FirstOrDefaultAsync(x => x.CommunityId == communityId, cancellationToken);

    public Task<CommunitySubscription?> GetByProviderSubscriptionIdTrackedAsync(string providerSubscriptionId,
        CancellationToken cancellationToken = default) =>
        _context.CommunitySubscriptions.FirstOrDefaultAsync(x => x.ProviderSubscriptionId == providerSubscriptionId,
            cancellationToken);

    public Task<CommunitySubscription?> GetByProviderCustomerIdTrackedAsync(string providerCustomerId,
        CancellationToken cancellationToken = default) =>
        _context.CommunitySubscriptions.FirstOrDefaultAsync(x => x.ProviderCustomerId == providerCustomerId,
            cancellationToken);

    public Task AddAsync(CommunitySubscription subscription, CancellationToken cancellationToken = default) =>
        _context.CommunitySubscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public void Update(CommunitySubscription subscription) =>
        _context.CommunitySubscriptions.Update(subscription);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
