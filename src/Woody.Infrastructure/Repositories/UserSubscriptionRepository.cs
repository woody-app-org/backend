using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly WoodyDbContext _context;

    public UserSubscriptionRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<UserSubscription?> GetByUserIdNoTrackingAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.UserSubscriptions.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<UserSubscription?> GetByUserIdTrackedAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.UserSubscriptions.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default) =>
        _context.UserSubscriptions.AddAsync(subscription, cancellationToken).AsTask();

    public void Update(UserSubscription subscription) =>
        _context.UserSubscriptions.Update(subscription);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
