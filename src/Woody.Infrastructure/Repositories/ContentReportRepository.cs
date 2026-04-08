using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories;

public class ContentReportRepository : IContentReportRepository
{
    private readonly WoodyDbContext _db;

    public ContentReportRepository(WoodyDbContext db)
    {
        _db = db;
    }

    public void Add(ContentReport report) => _db.ContentReports.Add(report);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
