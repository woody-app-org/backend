using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUsernameHistoryRepository
{
    Task AddAsync(UsernameHistory entry, CancellationToken cancellationToken = default);

    /// <summary>Resolve utilizador actual a partir de um username antigo (OldUsername).</summary>
    Task<int?> GetUserIdByOldUsernameAsync(string oldUsername, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
