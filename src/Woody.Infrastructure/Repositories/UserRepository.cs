using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly WoodyDbContext _context;

    public UserRepository(WoodyDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByUsernameOrEmailAsync(string login)
    {
        var normalized = login.Trim();
        if (normalized.Contains('@', StringComparison.Ordinal))
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized.ToLowerInvariant());

        return await _context.Users.FirstOrDefaultAsync(u => u.Username == normalized);
    }

    public async Task<User?> GetByIdAsync(int id) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<bool> ExistsUsernameAsync(string username) =>
        await _context.Users.AnyAsync(u => u.Username == username);

    public async Task<bool> ExistsEmailAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLowerInvariant());

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public void Update(User user) => _context.Users.Update(user);
}
