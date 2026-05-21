using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class UsernameHistoryRepository : IUsernameHistoryRepository
{
    private readonly WoodyDbContext _context;

    public UsernameHistoryRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UsernameHistory entry, CancellationToken cancellationToken = default) =>
        await _context.UsernameHistories.AddAsync(entry, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
