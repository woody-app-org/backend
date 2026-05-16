using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class LoginLockoutRepository : ILoginLockoutRepository
{
    private readonly WoodyDbContext _context;

    public LoginLockoutRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public Task<LoginLockout?> GetByNormalizedLoginAsync(
        string normalizedLogin,
        CancellationToken cancellationToken = default) =>
        _context.LoginLockouts.FirstOrDefaultAsync(x => x.NormalizedLogin == normalizedLogin, cancellationToken);

    public void Add(LoginLockout lockout) => _context.LoginLockouts.Add(lockout);

    public void Remove(LoginLockout lockout) => _context.LoginLockouts.Remove(lockout);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
