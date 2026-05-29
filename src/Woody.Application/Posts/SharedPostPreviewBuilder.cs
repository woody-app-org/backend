using Woody.Application.DTOs.Api;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Posts;

public static class SharedPostPreviewBuilder
{
    private const int MaxPreviewLength = 140;

    public static SharedPostPreviewDto Unavailable() => new() { IsUnavailable = true };

    public static SharedPostPreviewDto FromPost(Post post)
    {
        var content = (post.Content ?? string.Empty).Trim();
        if (content.Length > MaxPreviewLength)
            content = content[..MaxPreviewLength].TrimEnd() + "…";

        var firstMedia = post.MediaAttachments
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Id)
            .FirstOrDefault();

        var legacyUrl = post.ImageUrl;
        string? mediaUrl = firstMedia?.Url ?? legacyUrl;
        string? mediaType = firstMedia != null
            ? MediaKindToApiString(firstMedia.MediaKind)
            : legacyUrl != null ? "image" : null;

        return new SharedPostPreviewDto
        {
            Id = post.Id.ToString(),
            PublicId = post.PublicId,
            AuthorDisplayName = post.User.DisplayName,
            AuthorUsername = post.User.Username,
            AuthorProfilePic = post.User.ProfilePic,
            ContentPreview = string.IsNullOrEmpty(content) ? null : content,
            FirstMediaUrl = mediaUrl,
            FirstMediaType = mediaType,
            CommunityName = post.PublicationContext == PostPublicationContext.Community && post.Community != null
                ? post.Community.Name
                : null,
            IsUnavailable = false
        };
    }

    public static async Task<SharedPostPreviewDto> ForViewerAsync(
        Post? post,
        int? viewerUserId,
        IResourceAuthorizationService authorization,
        CancellationToken cancellationToken = default)
    {
        if (post == null || post.DeletedAt != null)
            return Unavailable();
        if (!await authorization.CanReadPostAsync(post, viewerUserId, cancellationToken))
            return Unavailable();
        return FromPost(post);
    }

    private static string MediaKindToApiString(MediaKind kind) => kind switch
    {
        MediaKind.Video => "video",
        MediaKind.Gif => "gif",
        MediaKind.Sticker => "sticker",
        _ => "image"
    };
}
