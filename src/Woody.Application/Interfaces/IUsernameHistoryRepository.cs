using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IUsernameHistoryRepository
{
    Task AddAsync(UsernameHistory entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
