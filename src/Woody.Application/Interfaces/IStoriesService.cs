using Woody.Application.DTOs.Api;

namespace Woody.Application.Interfaces;

public enum StoryOperationOutcome
{
    Success,
    InvalidMediaType,
    InvalidContent,
    InvalidUrl,
    LimitReached,
    NotFound,
    Forbidden
}

public sealed record StoryCommandResult(
    StoryOperationOutcome Outcome,
    StoryDto? Story = null,
    string? Error = null,
    string? Code = null);

public sealed record StoryViewsCommandResult(
    StoryOperationOutcome Outcome,
    IReadOnlyList<StoryViewDto>? Views = null,
    string? Error = null);

public interface IStoriesService
{
    Task<StoryCommandResult> CreateStoryAsync(
        int currentUserId,
        CreateStoryRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryDto>> GetActiveStoriesByUserAsync(
        int targetUserId,
        int? viewerUserId,
        CancellationToken cancellationToken = default);

    Task<StoryCommandResult> DeleteStoryAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default);

    Task<StoryCommandResult> RegisterViewAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default);

    Task<StoryViewsCommandResult> GetStoryViewsAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryFeedItemDto>> GetStoriesFeedAsync(
        int viewerUserId,
        CancellationToken cancellationToken = default);
}
