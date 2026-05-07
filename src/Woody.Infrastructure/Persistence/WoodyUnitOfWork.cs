using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence;

public class WoodyUnitOfWork : IWoodyUnitOfWork
{
    private readonly WoodyDbContext _db;

    public WoodyUnitOfWork(WoodyDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action();
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
