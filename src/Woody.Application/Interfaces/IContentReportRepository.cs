using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IContentReportRepository
{
    void Add(ContentReport report);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
