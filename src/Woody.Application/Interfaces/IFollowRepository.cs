using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IFollowRepository
{
    Task<bool> ExistsAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default);
    Task<List<int>> GetFollowedUserIdsAsync(int followingUserId, CancellationToken cancellationToken = default);
    Task<List<Follow>> ListFollowingWithFollowedUserAsync(int followingUserId, CancellationToken cancellationToken = default);
    void Add(Follow follow);
    Task<Follow?> GetAsync(int followingUserId, int followedUserId, CancellationToken cancellationToken = default);
    void Remove(Follow follow);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
