using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IFollowRepository
{
    /// <summary>True se A segue B e B segue A.</summary>
    Task<bool> AreMutualFollowersAsync(int userIdA, int userIdB, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default);
    Task<List<int>> GetFollowedUserIdsAsync(int followingUserId, CancellationToken cancellationToken = default);
    Task<int> CountFollowersAsync(int followedUserId, CancellationToken cancellationToken = default);
    Task<int> CountFollowingAsync(int followingUserId, CancellationToken cancellationToken = default);

    /// <summary>Utilizadores que seguem <paramref name="followedUserId"/>.</summary>
    Task<(List<User> Items, int Total)> ListFollowersPagedAsync(
        int followedUserId,
        int page,
        int pageSize,
        string? search = null,
        IReadOnlyCollection<int>? excludeUserIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Utilizadores seguidos por <paramref name="followingUserId"/>.</summary>
    Task<(List<User> Items, int Total)> ListFollowingPagedAsync(
        int followingUserId,
        int page,
        int pageSize,
        string? search = null,
        IReadOnlyCollection<int>? excludeUserIds = null,
        CancellationToken cancellationToken = default);

    void Add(Follow follow);
    Task<Follow?> GetAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default);
    void Remove(Follow follow);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
