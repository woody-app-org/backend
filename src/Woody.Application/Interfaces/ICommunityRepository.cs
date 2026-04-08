using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface ICommunityRepository
{
    Task<List<Community>> ListWithTagsOrderedByNameAsync(CancellationToken cancellationToken = default);
    Task<Community?> GetByIdWithTagsNoTrackingAsync(int id, CancellationToken cancellationToken = default);
    Task<Community?> GetBySlugWithTagsNoTrackingAsync(string slug, CancellationToken cancellationToken = default);
    Task<Community?> GetBySlugTrackedAsync(string slug, CancellationToken cancellationToken = default);
    Task<Community?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default);
    Task<Community?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Community>> SearchWithTagsAsync(string loweredQuery, int take, CancellationToken cancellationToken = default);
    void RemoveCommunityTags(IEnumerable<CommunityTag> tags);
    void AddCommunityTag(CommunityTag tag);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
