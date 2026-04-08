using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Domain.Entities;

namespace Woody.Application.Services;

public class PostEnrichmentService : IPostEnrichmentService
{
    private readonly ILikeRepository _likes;
    private readonly ICommentRepository _comments;

    public PostEnrichmentService(ILikeRepository likes, ICommentRepository comments)
    {
        _likes = likes;
        _comments = comments;
    }

    public async Task<List<PostResponseDto>> ToPostDtosAsync(
        IReadOnlyList<Post> posts,
        int? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        if (posts.Count == 0)
            return new List<PostResponseDto>();

        var ids = posts.Select(p => p.Id).ToList();

        var likeCounts = await _likes.GetPostLikeCountsAsync(ids, cancellationToken);
        var commentCounts = await _comments.GetActiveCommentCountsByPostIdsAsync(ids, cancellationToken);

        HashSet<int> liked = new();
        if (viewerUserId.HasValue)
            liked = await _likes.GetLikedPostIdsForViewerAsync(viewerUserId.Value, ids, cancellationToken);

        return posts.Select(p =>
        {
            var lc = likeCounts.GetValueOrDefault(p.Id);
            var cc = commentCounts.GetValueOrDefault(p.Id);
            var isLiked = viewerUserId.HasValue && liked.Contains(p.Id);
            return EntityMappers.ToPostDto(p, lc, cc, viewerUserId, isLiked);
        }).ToList();
    }
}
