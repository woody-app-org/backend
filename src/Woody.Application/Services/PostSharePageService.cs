using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.Interfaces;
using Woody.Application.Posts;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public sealed class PostSharePageService : IPostSharePageService
{
    private const int MaxTitleLength = 70;
    private const int MaxDescriptionLength = 160;

    private readonly IPostRepository _posts;
    private readonly IResourceAuthorizationService _authorization;
    private readonly PublicShareOptions _options;

    public PostSharePageService(
        IPostRepository posts,
        IResourceAuthorizationService authorization,
        IOptions<PublicShareOptions> options)
    {
        _posts = posts;
        _authorization = authorization;
        _options = options.Value;
    }

    public async Task<PostSharePageModel> BuildPageModelAsync(
        string publicId,
        string requestOrigin,
        CancellationToken cancellationToken = default)
    {
        var normalizedId = publicId?.Trim() ?? string.Empty;
        var shareBase = ResolveShareBaseUrl(requestOrigin);
        var apiOrigin = ResolveApiOrigin(requestOrigin);
        var frontendOrigin = NormalizeOrigin(_options.FrontendPublicOrigin, "http://localhost:5173");
        var fallbackImage = ResolveFallbackImageUrl(frontendOrigin);
        var sharePageUrl = string.IsNullOrEmpty(normalizedId)
            ? $"{shareBase}/share/posts/"
            : $"{shareBase}/share/posts/{Uri.EscapeDataString(normalizedId)}";
        var frontendPostUrl = string.IsNullOrEmpty(normalizedId)
            ? frontendOrigin
            : $"{frontendOrigin}/posts/{Uri.EscapeDataString(normalizedId)}";

        var generic = BuildGenericModel(sharePageUrl, frontendPostUrl, fallbackImage, normalizedId);

        if (string.IsNullOrWhiteSpace(normalizedId))
            return generic;

        var post = await _posts.GetByPublicIdNonDeletedWithNavAsync(normalizedId, cancellationToken);
        if (post == null)
            return generic;

        if (post.User == null || post.User.VerificationStatus != VerificationStatus.Approved)
            return generic;

        if (!await _authorization.CanReadPostAsync(post, viewerUserId: null, cancellationToken))
            return generic;

        return BuildRealModel(post, sharePageUrl, frontendPostUrl, fallbackImage, apiOrigin);
    }

    public string RenderHtml(PostSharePageModel model) => PostShareHtmlRenderer.Render(model);

    private PostSharePageModel BuildGenericModel(
        string sharePageUrl,
        string frontendPostUrl,
        string fallbackImage,
        string? publicId) =>
        new()
        {
            Title = "Woody",
            Description = "Conteúdo disponível apenas para quem tem acesso.",
            ImageUrl = fallbackImage,
            SharePageUrl = sharePageUrl,
            FrontendPostUrl = frontendPostUrl,
            IsUnavailable = true,
            PublicId = publicId
        };

    private PostSharePageModel BuildRealModel(
        Post post,
        string sharePageUrl,
        string frontendPostUrl,
        string fallbackImage,
        string apiOrigin)
    {
        var authorName = (post.User.DisplayName ?? post.User.Username ?? "Autora").Trim();
        var content = NormalizePreviewText(post.Content);
        var title = !string.IsNullOrWhiteSpace(content)
            ? Truncate(content, MaxTitleLength)
            : $"Publicação de {authorName} na Woody";
        if (title.Length > MaxTitleLength)
            title = Truncate(title, MaxTitleLength);

        var description = !string.IsNullOrWhiteSpace(content)
            ? Truncate(content, MaxDescriptionLength)
            : $"Publicação de {authorName} na Woody.";
        if (description.Length > MaxDescriptionLength)
            description = Truncate(description, MaxDescriptionLength);

        var imageUrl = ResolveOgImageUrl(post, apiOrigin, fallbackImage);

        return new PostSharePageModel
        {
            Title = title,
            Description = description,
            ImageUrl = imageUrl,
            SharePageUrl = sharePageUrl,
            FrontendPostUrl = frontendPostUrl,
            IsUnavailable = false,
            PublicId = post.PublicId
        };
    }

    private string ResolveOgImageUrl(Post post, string apiOrigin, string fallbackImage)
    {
        var firstMedia = post.MediaAttachments
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.Id)
            .FirstOrDefault();

        var candidate = PickMediaUrl(firstMedia, post.ImageUrl);
        var absolute = MakeAbsolutePublicUrl(candidate, apiOrigin);
        return IsCrawlerSafeImageUrl(absolute) ? absolute! : fallbackImage;
    }

    private static string? PickMediaUrl(MediaAttachment? attachment, string? legacyImageUrl)
    {
        if (attachment == null)
            return legacyImageUrl;

        if (attachment.MediaKind == MediaKind.Video || attachment.MediaKind == MediaKind.Gif)
        {
            if (!string.IsNullOrWhiteSpace(attachment.ThumbnailUrl))
                return attachment.ThumbnailUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(attachment.Url))
            return attachment.Url.Trim();

        return legacyImageUrl;
    }

    private static string? MakeAbsolutePublicUrl(string? url, string apiOrigin)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var trimmed = url.Trim();
        if (trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (trimmed.StartsWith('/'))
            return $"{apiOrigin}{trimmed}";

        return $"{apiOrigin}/{trimmed.TrimStart('/')}";
    }

    private static bool IsCrawlerSafeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        if (url.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;
        return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePreviewText(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var normalized = content
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');
        while (normalized.Contains("  ", StringComparison.Ordinal))
            normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
        return normalized.Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        return value[..maxLength].TrimEnd() + "…";
    }

    private string ResolveShareBaseUrl(string requestOrigin)
    {
        var configured = _options.PublicShareBaseUrl?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return NormalizeOrigin(configured, requestOrigin);
        return NormalizeOrigin(requestOrigin, "http://localhost:5000");
    }

    private string ResolveApiOrigin(string requestOrigin)
    {
        var configured = _options.ApiPublicOrigin?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return NormalizeOrigin(configured, requestOrigin);
        return NormalizeOrigin(requestOrigin, "http://localhost:5000");
    }

    private string ResolveFallbackImageUrl(string frontendOrigin)
    {
        var configured = _options.OgFallbackImageUrl?.Trim();
        if (!string.IsNullOrEmpty(configured))
            return configured;
        return $"{frontendOrigin}/icon-512.png";
    }

    private static string NormalizeOrigin(string value, string fallback)
    {
        var trimmed = value?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(trimmed))
            trimmed = fallback.Trim().TrimEnd('/');
        return trimmed;
    }
}
