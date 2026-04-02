using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByUsernameOrEmailAsync(string login);
    Task<User?> GetByIdAsync(int id);
    Task<bool> ExistsUsernameAsync(string username);
    Task<bool> ExistsEmailAsync(string email);
    Task AddAsync(User user);
    Task SaveChangesAsync();
    void Update(User user);
}
