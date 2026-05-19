using System.Data;
using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Application.Stories;
using Woody.Domain.Entities;
using Woody.Domain.Stories;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class StoryRepository : IStoryRepository
{
    private readonly WoodyDbContext _db;

    public StoryRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public Task<Story?> GetActiveByIdAsync(int storyId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return ActiveQuery(now)
            .Include(s => s.Author).ThenInclude(u => u.Subscription)
            .FirstOrDefaultAsync(s => s.Id == storyId, cancellationToken);
    }

    public Task<Story?> GetByIdIncludingDeletedAsync(int storyId, CancellationToken cancellationToken = default) =>
        _db.Stories
            .Include(s => s.Author)
            .FirstOrDefaultAsync(s => s.Id == storyId, cancellationToken);

    public Task<List<Story>> ListActiveByAuthorAsync(int authorUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return ActiveQuery(now)
            .Where(s => s.AuthorUserId == authorUserId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetUserIdsWithActiveStoriesAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        var withStories = await ActiveQuery(now)
            .Where(s => ids.Contains(s.AuthorUserId))
            .Select(s => s.AuthorUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return withStories.ToHashSet();
    }

    public Task<bool> HasActiveStoriesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return ActiveQuery(now).AnyAsync(s => s.AuthorUserId == userId, cancellationToken);
    }

    public async Task<Story> CreateWithActiveLimitAsync(Story story, CancellationToken cancellationToken = default)
    {
        // SQLite/testes: mutex por usuária. PostgreSQL: pg_advisory_xact_lock dentro da transação.
        await using var nonPgUserMutex = _db.Database.IsNpgsql()
            ? null
            : await StoryUserCreationMutex.AcquireAsync(story.AuthorUserId, cancellationToken);

        var useExplicitTransaction = _db.Database.IsRelational();
        await using var transaction = useExplicitTransaction
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        await AcquirePgCreationLockAsync(story.AuthorUserId, cancellationToken);

        var now = DateTime.UtcNow;
        var activeCount = await CountActiveStoriesAsync(story.AuthorUserId, now, cancellationToken);

        if (activeCount >= StoryPolicies.MaxActiveStoriesPerUser)
            throw new StoryLimitReachedException();

        _db.Stories.Add(story);
        await _db.SaveChangesAsync(cancellationToken);

        if (transaction != null)
            await transaction.CommitAsync(cancellationToken);

        return story;
    }

    public async Task<bool> SoftDeleteAsync(Story story, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
    {
        if (story.DeletedAt != null)
            return false;

        story.DeletedAt = deletedAtUtc;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRegisterViewAsync(
        int storyId,
        int viewerUserId,
        DateTime viewedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var exists = await _db.StoryViews.AsNoTracking()
            .AnyAsync(v => v.StoryId == storyId && v.ViewerUserId == viewerUserId, cancellationToken);
        if (exists)
            return false;

        _db.StoryViews.Add(new StoryView
        {
            StoryId = storyId,
            ViewerUserId = viewerUserId,
            ViewedAt = viewedAtUtc
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public Task<List<StoryView>> ListViewsForStoryAsync(int storyId, CancellationToken cancellationToken = default) =>
        _db.StoryViews.AsNoTracking()
            .Include(v => v.Viewer).ThenInclude(u => u.Subscription)
            .Where(v => v.StoryId == storyId)
            .OrderByDescending(v => v.ViewedAt)
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<int, int>> GetViewCountsByStoryIdsAsync(
        IEnumerable<int> storyIds,
        CancellationToken cancellationToken = default)
    {
        var ids = storyIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, int>();

        return await _db.StoryViews.AsNoTracking()
            .Where(v => ids.Contains(v.StoryId))
            .GroupBy(v => v.StoryId)
            .Select(g => new { StoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoryId, x => x.Count, cancellationToken);
    }

    public async Task<HashSet<int>> GetStoryIdsViewedByUserAsync(
        int viewerUserId,
        IEnumerable<int> storyIds,
        CancellationToken cancellationToken = default)
    {
        var ids = storyIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var viewed = await _db.StoryViews.AsNoTracking()
            .Where(v => v.ViewerUserId == viewerUserId && ids.Contains(v.StoryId))
            .Select(v => v.StoryId)
            .ToListAsync(cancellationToken);

        return viewed.ToHashSet();
    }

    public async Task<List<StoryFeedAuthorSummary>> ListActiveStoryAuthorsByUserIdsAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var now = DateTime.UtcNow;
        var grouped = await ActiveQuery(now)
            .Where(s => ids.Contains(s.AuthorUserId))
            .GroupBy(s => s.AuthorUserId)
            .Select(g => new
            {
                AuthorUserId = g.Key,
                LastCreatedAt = g.Max(s => s.CreatedAt),
                StoryIds = g.Select(s => s.Id)
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new StoryFeedAuthorSummary
            {
                AuthorUserId = g.AuthorUserId,
                LastCreatedAt = g.LastCreatedAt,
                StoryIds = g.StoryIds.ToList()
            })
            .ToList();
    }

    private Task<int> CountActiveStoriesAsync(int authorUserId, DateTime utcNow, CancellationToken cancellationToken) =>
        ActiveQuery(utcNow)
            .Where(s => s.AuthorUserId == authorUserId)
            .CountAsync(cancellationToken);

    private IQueryable<Story> ActiveQuery(DateTime utcNow) =>
        _db.Stories.AsNoTracking()
            .Where(s => s.DeletedAt == null && s.ExpiresAt > utcNow);

    /// <summary>Deve ser chamado após <see cref="DatabaseFacade.BeginTransactionAsync"/> (lock de transação).</summary>
    private async Task AcquirePgCreationLockAsync(int userId, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsNpgsql())
            return;

        await _db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0})",
            [userId],
            cancellationToken);
    }
}
