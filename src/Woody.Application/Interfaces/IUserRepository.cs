using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByUsernameOrEmailAsync(string login);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdNoTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsNoTrackingAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithSocialLinksAndInterestsNoTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<List<UserInterest>> GetInterestsTrackedByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    void RemoveUserInterests(IEnumerable<UserInterest> interests);
    void AddUserInterest(UserInterest interest);
    Task<List<User>> SearchUsersNoTrackingAsync(
        string loweredQuery,
        int take,
        IReadOnlyCollection<int>? excludeUserIds = null,
        CancellationToken cancellationToken = default);
    Task<List<User>> ListUsersForSuggestionsAsync(IReadOnlyCollection<int> excludeUserIds, int take, CancellationToken cancellationToken = default);
    Task<bool> ExistsUsernameAsync(string username);
    Task<bool> ExistsEmailAsync(string email);
    Task<bool> ExistsCpfAsync(string cpfDigits, CancellationToken cancellationToken = default);
    Task AddAsync(User user);
    Task SaveChangesAsync();
    void Update(User user);
}
