using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IPostRepository
{
    Task<List<Post>> ListNonDeletedWithNavAsync(CancellationToken cancellationToken = default);
    Task<(List<Post> Items, int Total)> ListByUserIdPagedAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(List<Post> Items, int Total)> ListByCommunityIdPagedAsync(int communityId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdNonDeletedWithNavAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdNonDeletedForCommentLookupAsync(int id, CancellationToken cancellationToken = default);
    void Add(Post post);
    Task AddPostTagsAsync(IEnumerable<PostTag> tags, CancellationToken cancellationToken = default);
    Task AddPostImagesAsync(IEnumerable<PostImage> images, CancellationToken cancellationToken = default);
    void RemovePostTags(IEnumerable<PostTag> tags);
    Task<List<Post>> SearchNonDeletedWithNavAsync(string loweredQuery, int take, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
