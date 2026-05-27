using Microsoft.EntityFrameworkCore;
using Npgsql;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class BadgeRepository : IBadgeRepository
{
    private const string UniqueViolationSqlState = "23505";
    private const string UserBadgeUniqueIndexName = "ix_user_badges_user_id_badge_id";

    private readonly WoodyDbContext _db;

    public BadgeRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<Badge?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
        _db.Badges.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);

    public Task<List<UserBadge>> GetActiveUserBadgesAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.UserBadges.AsNoTracking()
            .Include(ub => ub.Badge)
            .Where(ub => ub.UserId == userId && ub.Badge.IsActive)
            .OrderBy(ub => ub.Badge.SortOrder)
            .ThenBy(ub => ub.EarnedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> UserHasBadgeAsync(int userId, string badgeSlug, CancellationToken cancellationToken = default) =>
        _db.UserBadges.AsNoTracking()
            .AnyAsync(
                ub => ub.UserId == userId && ub.Badge.Slug == badgeSlug,
                cancellationToken);

    public async Task<bool> TryAddUserBadgeAsync(
        int userId,
        int badgeId,
        DateTime earnedAt,
        CancellationToken cancellationToken = default)
    {
        if (await _db.UserBadges.AsNoTracking()
                .AnyAsync(ub => ub.UserId == userId && ub.BadgeId == badgeId, cancellationToken))
        {
            return false;
        }

        var userBadge = new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId,
            EarnedAt = earnedAt
        };

        _db.UserBadges.Add(userBadge);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueUserBadgeViolation(ex))
        {
            Detach(userBadge);
            return false;
        }
    }

    private static bool IsUniqueUserBadgeViolation(DbUpdateException ex) =>
        IsPostgresUniqueUserBadgeViolation(ex) || IsSqliteUniqueViolation(ex);

    private static bool IsPostgresUniqueUserBadgeViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pg
        && pg.SqlState == UniqueViolationSqlState
        && (pg.ConstraintName == null
            || string.Equals(pg.ConstraintName, UserBadgeUniqueIndexName, StringComparison.OrdinalIgnoreCase));

    private static bool IsSqliteUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException?.GetType().FullName != "Microsoft.Data.Sqlite.SqliteException")
            return false;

        var errorCode = ex.InnerException.GetType().GetProperty("SqliteErrorCode")?.GetValue(ex.InnerException);
        return errorCode is 19;
    }

    private void Detach(UserBadge userBadge)
    {
        var entry = _db.Entry(userBadge);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }
}
