using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Application.Validation;
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

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = UsernameInputValidator.Normalize(username);
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == normalized);
    }

    public async Task<User?> GetByUsernameOrEmailAsync(string login)
    {
        var normalized = login.Trim();
        if (normalized.Contains('@', StringComparison.Ordinal))
            return await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized.ToLowerInvariant());

        return await _context.Users.FirstOrDefaultAsync(u => u.Username == UsernameInputValidator.Normalize(normalized));
    }

    public async Task<User?> GetByIdAsync(int id) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<List<User>> GetByIdsNoTrackingAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return [];

        return await _context.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdWithSocialLinksAndInterestsNoTrackingAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Include(x => x.Subscription)
            .Include(x => x.SocialLinks)
            .Include(x => x.Interests)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<User?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<List<UserInterest>> GetInterestsTrackedByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        await _context.UserInterests.Where(i => i.UserId == userId).ToListAsync(cancellationToken);

    public void RemoveUserInterests(IEnumerable<UserInterest> interests) =>
        _context.UserInterests.RemoveRange(interests);

    public void AddUserInterest(UserInterest interest) =>
        _context.UserInterests.Add(interest);

    public async Task<List<User>> SearchUsersNoTrackingAsync(string loweredQuery, int take, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Include(u => u.Subscription)
            .Where(u => u.Username.ToLower().Contains(loweredQuery)
                        || (u.DisplayName != null && u.DisplayName.ToLower().Contains(loweredQuery))
                        || u.Email.ToLower().Contains(loweredQuery))
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<List<User>> ListUsersForSuggestionsAsync(IReadOnlyCollection<int> excludeUserIds, int take, CancellationToken cancellationToken = default) =>
        await _context.Users.AsNoTracking()
            .Include(u => u.Subscription)
            .Where(u => !excludeUserIds.Contains(u.Id))
            .OrderBy(u => u.DisplayName ?? u.Username)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsUsernameAsync(string username)
    {
        var normalized = UsernameInputValidator.Normalize(username);
        return await _context.Users.AnyAsync(u => u.Username == normalized);
    }

    public async Task<bool> ExistsEmailAsync(string email) =>
        await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLowerInvariant());

    public async Task<bool> ExistsCpfAsync(string cpfDigits, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(cpfDigits))
            return false;

        return await _context.Users.AnyAsync(u => u.Cpf == cpfDigits, cancellationToken);
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public void Update(User user) => _context.Users.Update(user);
}
