using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public interface IFeedService
{
    Task<PaginatedResponseDto<PostResponseDto>> GetFeedAsync(
        int page,
        int pageSize,
        string filter,
        int? viewerUserId,
        CancellationToken cancellationToken = default);
}
