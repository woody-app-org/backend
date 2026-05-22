using System.Text.RegularExpressions;
using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Application.Mapping;
using Woody.Application.Stories;
using Woody.Application.Validation;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Stories;

namespace Woody.Application.Services;

public class StoriesService : IStoriesService
{
    private static readonly Regex HexColorRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly IStoryRepository _stories;
    private readonly IUserRepository _users;
    private readonly IFollowRepository _follows;
    private readonly IMediaStorageProvider _mediaStorage;
    private readonly IProfileSignalSocialGate _socialGate;

    public StoriesService(
        IStoryRepository stories,
        IUserRepository users,
        IFollowRepository follows,
        IMediaStorageProvider mediaStorage,
        IProfileSignalSocialGate socialGate)
    {
        _stories = stories;
        _users = users;
        _follows = follows;
        _mediaStorage = mediaStorage;
        _socialGate = socialGate;
    }

    public async Task<StoryCommandResult> CreateStoryAsync(
        int currentUserId,
        CreateStoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseMediaType(request.MediaType, out var mediaType))
            return Failure(StoryOperationOutcome.InvalidMediaType, "Tipo de mídia inválido. Use image, video ou text.");

        if (!TryBuildStoryContent(request, mediaType, out var content, out var error))
            return Failure(StoryOperationOutcome.InvalidContent, error!);

        var author = await _users.GetByIdNoTrackingAsync(currentUserId, cancellationToken);
        if (author == null)
            return Failure(StoryOperationOutcome.NotFound, "Utilizadora não encontrada.");

        var now = DateTime.UtcNow;
        var story = new Story
        {
            AuthorUserId = currentUserId,
            MediaType = mediaType,
            MediaUrl = content.MediaUrl,
            ThumbnailUrl = content.ThumbnailUrl,
            StorageKey = content.StorageKey,
            Text = content.Text,
            BackgroundColor = content.BackgroundColor,
            CreatedAt = now,
            ExpiresAt = now.Add(StoryPolicies.StoryLifetime),
            DeletedAt = null,
            Visibility = StoryVisibility.Public
        };

        try
        {
            await _stories.CreateWithActiveLimitAsync(story, cancellationToken);
        }
        catch (StoryLimitReachedException ex)
        {
            return Failure(
                StoryOperationOutcome.LimitReached,
                ex.Message,
                StoryLimitReachedException.ErrorCode);
        }

        story.Author = author;
        return new StoryCommandResult(
            StoryOperationOutcome.Success,
            MapStory(story, viewCount: 0, hasViewedByMe: false));
    }

    public async Task<IReadOnlyList<StoryDto>> GetActiveStoriesByUserAsync(
        int targetUserId,
        int? viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var authorExists = await _users.GetByIdNoTrackingAsync(targetUserId, cancellationToken);
        if (authorExists == null)
            return [];

        if (viewerUserId.HasValue && viewerUserId.Value != targetUserId)
        {
            if (await _socialGate.AreUsersBlockedEitherWayAsync(viewerUserId.Value, targetUserId, cancellationToken))
                return [];
        }

        var stories = await _stories.ListActiveByAuthorAsync(targetUserId, cancellationToken);
        if (stories.Count == 0)
            return [];

        foreach (var story in stories)
            story.Author = authorExists;

        var storyIds = stories.Select(s => s.Id).ToList();
        var viewCounts = await _stories.GetViewCountsByStoryIdsAsync(storyIds, cancellationToken);
        HashSet<int> viewedByMe = [];
        if (viewerUserId.HasValue)
            viewedByMe = await _stories.GetStoryIdsViewedByUserAsync(viewerUserId.Value, storyIds, cancellationToken);

        var dtos = new List<StoryDto>(stories.Count);
        foreach (var story in stories)
        {
            viewCounts.TryGetValue(story.Id, out var count);
            dtos.Add(MapStory(story, count, viewedByMe.Contains(story.Id)));
        }

        return dtos;
    }

    public async Task<StoryCommandResult> DeleteStoryAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default)
    {
        var story = await _stories.GetByIdIncludingDeletedAsync(storyId, cancellationToken);
        if (story == null || story.DeletedAt != null)
            return Failure(StoryOperationOutcome.NotFound, "Story não encontrado.");

        if (!await CanManageStoryAsync(story, currentUserId, cancellationToken))
            return Failure(StoryOperationOutcome.NotFound, "Story não encontrado.");

        var now = DateTime.UtcNow;
        if (story.ExpiresAt <= now)
            return Failure(StoryOperationOutcome.NotFound, "Story não encontrado.");

        await _stories.SoftDeleteAsync(story, now, cancellationToken);

        if (!string.IsNullOrWhiteSpace(story.StorageKey))
            await _mediaStorage.TryDeleteAsync(story.StorageKey, cancellationToken);

        return new StoryCommandResult(StoryOperationOutcome.Success);
    }

    public async Task<StoryCommandResult> RegisterViewAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default)
    {
        var story = await _stories.GetActiveByIdAsync(storyId, cancellationToken);
        if (story == null)
            return Failure(StoryOperationOutcome.NotFound, "Story não encontrado.");

        if (!await CanViewStoryAsync(story, currentUserId, cancellationToken))
            return Failure(StoryOperationOutcome.NotFound, "Story não encontrado.");

        await _stories.TryRegisterViewAsync(storyId, currentUserId, DateTime.UtcNow, cancellationToken);
        return new StoryCommandResult(StoryOperationOutcome.Success);
    }

    public async Task<StoryViewsCommandResult> GetStoryViewsAsync(
        int currentUserId,
        int storyId,
        CancellationToken cancellationToken = default)
    {
        var story = await _stories.GetActiveByIdAsync(storyId, cancellationToken);
        if (story == null)
            return new StoryViewsCommandResult(StoryOperationOutcome.NotFound, null, "Story não encontrado.");

        if (story.AuthorUserId != currentUserId)
            return new StoryViewsCommandResult(StoryOperationOutcome.Forbidden, null, "Não autorizado.");

        var views = await _stories.ListViewsForStoryAsync(storyId, cancellationToken);
        var dtos = views.Select(v => new StoryViewDto
        {
            ViewerUserId = v.ViewerUserId,
            DisplayName = v.Viewer.DisplayName ?? v.Viewer.Username,
            Username = v.Viewer.Username,
            AvatarUrl = v.Viewer.ProfilePic,
            ViewedAt = EntityMappers.Iso(v.ViewedAt)
        }).ToList();

        return new StoryViewsCommandResult(StoryOperationOutcome.Success, dtos);
    }

    public async Task<IReadOnlyList<StoryFeedItemDto>> GetStoriesFeedAsync(
        int viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var followedIds = await _follows.GetFollowedUserIdsAsync(viewerUserId, cancellationToken);
        var candidateIds = new HashSet<int>(followedIds) { viewerUserId };

        var authorSummaries = await _stories.ListActiveStoryAuthorsByUserIdsAsync(candidateIds, cancellationToken);
        if (authorSummaries.Count == 0)
            return [];

        var visibleSummaries = new List<StoryFeedAuthorSummary>(authorSummaries.Count);
        foreach (var summary in authorSummaries)
        {
            if (summary.AuthorUserId == viewerUserId)
            {
                visibleSummaries.Add(summary);
                continue;
            }

            if (await _socialGate.AreUsersBlockedEitherWayAsync(viewerUserId, summary.AuthorUserId, cancellationToken))
                continue;

            visibleSummaries.Add(summary);
        }

        if (visibleSummaries.Count == 0)
            return [];

        var allStoryIds = visibleSummaries.SelectMany(s => s.StoryIds).Distinct().ToList();
        var viewedStoryIds = await _stories.GetStoryIdsViewedByUserAsync(viewerUserId, allStoryIds, cancellationToken);

        var authorIds = visibleSummaries.Select(s => s.AuthorUserId).Distinct().ToList();
        var users = await _users.GetByIdsNoTrackingAsync(authorIds, cancellationToken);
        var userById = users.ToDictionary(u => u.Id);

        var items = new List<StoryFeedItemDto>(visibleSummaries.Count);
        foreach (var summary in visibleSummaries)
        {
            if (!userById.TryGetValue(summary.AuthorUserId, out var user))
                continue;

            var isSelf = summary.AuthorUserId == viewerUserId;
            var hasUnviewed = !isSelf && summary.StoryIds.Any(id => !viewedStoryIds.Contains(id));

            items.Add(new StoryFeedItemDto
            {
                UserId = user.Id.ToString(),
                DisplayName = user.DisplayName ?? user.Username,
                Username = user.Username,
                AvatarUrl = user.ProfilePic,
                HasActiveStories = true,
                HasUnviewedStories = hasUnviewed,
                LastStoryCreatedAt = EntityMappers.Iso(summary.LastCreatedAt),
                IsSelf = isSelf
            });
        }

        return items
            .OrderByDescending(i => i.IsSelf)
            .ThenByDescending(i => i.LastStoryCreatedAt)
            .ToList();
    }

    private async Task<StoryDto> MapStoryAsync(Story story, int? viewerUserId, CancellationToken cancellationToken)
    {
        var viewCounts = await _stories.GetViewCountsByStoryIdsAsync([story.Id], cancellationToken);
        viewCounts.TryGetValue(story.Id, out var count);
        var hasViewed = false;
        if (viewerUserId.HasValue)
        {
            var viewed = await _stories.GetStoryIdsViewedByUserAsync(viewerUserId.Value, [story.Id], cancellationToken);
            hasViewed = viewed.Contains(story.Id);
        }

        return MapStory(story, count, hasViewed);
    }

    private static StoryDto MapStory(Story story, int viewCount, bool hasViewedByMe) => new()
    {
        Id = story.Id,
        AuthorUserId = story.AuthorUserId,
        Author = EntityMappers.ToUserPublicDto(story.Author),
        MediaType = ToMediaTypeApi(story.MediaType),
        MediaUrl = story.MediaUrl,
        ThumbnailUrl = story.ThumbnailUrl,
        Text = story.Text,
        BackgroundColor = story.BackgroundColor,
        CreatedAt = EntityMappers.Iso(story.CreatedAt),
        ExpiresAt = EntityMappers.Iso(story.ExpiresAt),
        ViewCount = viewCount,
        HasViewedByMe = hasViewedByMe
    };

    private async Task<bool> CanManageStoryAsync(Story story, int actorUserId, CancellationToken cancellationToken)
    {
        if (story.AuthorUserId == actorUserId)
            return true;

        var actor = await _users.GetByIdNoTrackingAsync(actorUserId, cancellationToken);
        return string.Equals(actor?.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<bool> CanViewStoryAsync(Story story, int viewerUserId, CancellationToken cancellationToken)
    {
        // MVP: perfis públicos; ponto central para privacidade/bloqueio futuro.
        if (viewerUserId == story.AuthorUserId)
            return true;

        if (await _socialGate.AreUsersBlockedEitherWayAsync(viewerUserId, story.AuthorUserId, cancellationToken))
            return false;

        return story.Visibility == StoryVisibility.Public;
    }

    private static bool TryParseMediaType(string? raw, out StoryMediaType mediaType)
    {
        mediaType = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw.Trim().ToLowerInvariant() switch
        {
            "image" => Assign(StoryMediaType.Image, out mediaType),
            "video" => Assign(StoryMediaType.Video, out mediaType),
            "text" => Assign(StoryMediaType.Text, out mediaType),
            _ => false
        };
    }

    private static bool Assign(StoryMediaType value, out StoryMediaType mediaType)
    {
        mediaType = value;
        return true;
    }

    private static bool TryBuildStoryContent(
        CreateStoryRequestDto request,
        StoryMediaType mediaType,
        out StoryContentFields content,
        out string? error)
    {
        content = new StoryContentFields();
        error = null;

        switch (mediaType)
        {
            case StoryMediaType.Image:
                if (!TryNormalizeStoryImageUrl(request.MediaUrl, out var imageUrl, out error))
                    return false;
                if (imageUrl == null)
                {
                    error = "URL de imagem é obrigatória para story de imagem.";
                    return false;
                }

                content.MediaUrl = imageUrl;
                content.StorageKey = NormalizeStorageKey(request.StorageKey);
                return true;

            case StoryMediaType.Video:
                if (!InputValidator.TryNormalizeHttpsVideoUrl(request.MediaUrl, out var videoUrl, out error))
                    return false;
                if (videoUrl == null)
                {
                    error = "URL de vídeo é obrigatória para story de vídeo.";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl))
                {
                    if (!InputValidator.TryNormalizeHttpsImageUrl(request.ThumbnailUrl, out var thumb, out error))
                        return false;
                    content.ThumbnailUrl = thumb;
                }

                content.MediaUrl = videoUrl;
                content.StorageKey = NormalizeStorageKey(request.StorageKey);
                return true;

            case StoryMediaType.Text:
                if (!InputValidator.TryNormalizeRequiredText(
                        request.Text,
                        "Texto",
                        StoryPolicies.MaxTextLength,
                        out var text,
                        out error))
                    return false;

                content.Text = text;
                if (!TryNormalizeBackgroundColor(request.BackgroundColor, out var bg, out error))
                    return false;
                content.BackgroundColor = bg;
                return true;

            default:
                error = "Tipo de mídia inválido.";
                return false;
        }
    }

    private static bool TryNormalizeStoryImageUrl(string? raw, out string? normalized, out string? error)
    {
        if (!InputValidator.TryNormalizeHttpsImageUrl(raw, out normalized, out error))
            return false;

        if (normalized == null)
        {
            error = InputValidator.InvalidImageUrlMessage;
            return false;
        }

        return true;
    }

    private static bool TryNormalizeBackgroundColor(string? raw, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        var value = raw.Trim();
        if (value.Length > StoryPolicies.MaxBackgroundColorLength)
        {
            error = $"Cor de fundo não pode exceder {StoryPolicies.MaxBackgroundColorLength} caracteres.";
            return false;
        }

        if (!HexColorRegex.IsMatch(value))
        {
            error = "Cor de fundo inválida. Use formato hexadecimal (#RRGGBB).";
            return false;
        }

        normalized = value;
        return true;
    }

    private static string? NormalizeStorageKey(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static string ToMediaTypeApi(StoryMediaType mediaType) => mediaType switch
    {
        StoryMediaType.Image => "image",
        StoryMediaType.Video => "video",
        StoryMediaType.Text => "text",
        _ => mediaType.ToString().ToLowerInvariant()
    };

    private static StoryCommandResult Failure(
        StoryOperationOutcome outcome,
        string error,
        string? code = null) =>
        new(outcome, null, error, code);

    private sealed class StoryContentFields
    {
        public string? MediaUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? StorageKey { get; set; }
        public string? Text { get; set; }
        public string? BackgroundColor { get; set; }
    }
}
