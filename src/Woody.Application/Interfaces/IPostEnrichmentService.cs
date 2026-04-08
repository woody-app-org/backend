using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IPostEnrichmentService
{
    Task<List<PostResponseDto>> ToPostDtosAsync(
        IReadOnlyList<Post> posts,
        int? viewerUserId,
        CancellationToken cancellationToken = default);
}
