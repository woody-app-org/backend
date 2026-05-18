using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IStoryRepository
{
    Task<Story?> GetActiveByIdAsync(int storyId, CancellationToken cancellationToken = default);

    Task<Story?> GetByIdIncludingDeletedAsync(int storyId, CancellationToken cancellationToken = default);

    Task<List<Story>> ListActiveByAuthorAsync(int authorUserId, CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetUserIdsWithActiveStoriesAsync(
        IEnumerable<int> userIds,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveStoriesAsync(int userId, CancellationToken cancellationToken = default);

    Task<Story> CreateWithActiveLimitAsync(Story story, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Story story, DateTime deletedAtUtc, CancellationToken cancellationToken = default);

    Task<bool> TryRegisterViewAsync(int storyId, int viewerUserId, DateTime viewedAtUtc, CancellationToken cancellationToken = default);

    Task<List<StoryView>> ListViewsForStoryAsync(int storyId, CancellationToken cancellationToken = default);

    Task<Dictionary<int, int>> GetViewCountsByStoryIdsAsync(
        IEnumerable<int> storyIds,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetStoryIdsViewedByUserAsync(
        int viewerUserId,
        IEnumerable<int> storyIds,
        CancellationToken cancellationToken = default);
}
