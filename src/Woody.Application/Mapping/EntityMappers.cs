using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;
using Woody.Domain.Subscription;

namespace Woody.Application.Mapping;

public static class EntityMappers
{
    public static string Iso(DateTime dt) => dt.ToUniversalTime().ToString("o");

    public static UserPublicDto ToUserPublicDto(User u)
    {
        var utcNow = DateTime.UtcNow;
        return new UserPublicDto
        {
            Id = u.Id.ToString(),
            Name = u.DisplayName ?? u.Username,
            Username = u.Username,
            AvatarUrl = u.ProfilePic,
            Bio = u.Bio,
            Pronouns = u.Pronouns,
            ShowProBadge = SubscriptionEntitlement.ShouldShowProBadge(u.Subscription, utcNow)
        };
    }

    public static string ToProfileSignalTypeApi(ProfileSignalType type) => type switch
    {
        ProfileSignalType.TeNotei => "te_notei",
        ProfileSignalType.Olhadinha => "olhadinha",
        ProfileSignalType.ConhecerMais => "conhecer_mais",
        ProfileSignalType.QueroConversar => "quero_conversar",
        ProfileSignalType.CrushFofo => "crush_fofo",
        ProfileSignalType.Atracao => "atracao",
        ProfileSignalType.SinalVerde => "sinal_verde",
        ProfileSignalType.Cheguei => "cheguei",
        _ => type.ToString()
    };

    public static string ToProfileSignalLabel(ProfileSignalType type) => type switch
    {
        ProfileSignalType.TeNotei => "Te notei",
        ProfileSignalType.Olhadinha => "Olhadinha",
        ProfileSignalType.ConhecerMais => "Conhecer mais",
        ProfileSignalType.QueroConversar => "Quero conversar",
        ProfileSignalType.CrushFofo => "Crush fofo",
        ProfileSignalType.Atracao => "Atração",
        ProfileSignalType.SinalVerde => "Sinal verde",
        ProfileSignalType.Cheguei => "Cheguei",
        _ => type.ToString()
    };

    public static string ToProfileSignalEmoji(ProfileSignalType type) => type switch
    {
        ProfileSignalType.TeNotei => "👀",
        ProfileSignalType.Olhadinha => "😉",
        ProfileSignalType.ConhecerMais => "🍷",
        ProfileSignalType.QueroConversar => "✨",
        ProfileSignalType.CrushFofo => "🐻",
        ProfileSignalType.Atracao => "🔥",
        ProfileSignalType.SinalVerde => "✅",
        ProfileSignalType.Cheguei => "😏",
        _ => type.ToString()
    };

    public static string ToProfileSignalStatusApi(ProfileSignalStatus status) => status switch
    {
        ProfileSignalStatus.Sent => "sent",
        ProfileSignalStatus.Read => "read",
        ProfileSignalStatus.Archived => "archived",
        ProfileSignalStatus.Dismissed => "dismissed",
        _ => status.ToString()
    };

    public static ProfileSignalResponseDto ToProfileSignalDto(ProfileSignal signal) => new()
    {
        Id = signal.Id,
        Type = ToProfileSignalTypeApi(signal.Type),
        Label = ToProfileSignalLabel(signal.Type),
        Emoji = ToProfileSignalEmoji(signal.Type),
        Message = signal.Message,
        Status = ToProfileSignalStatusApi(signal.Status),
        CreatedAt = Iso(signal.CreatedAt),
        ReadAt = signal.ReadAt.HasValue ? Iso(signal.ReadAt.Value) : null,
        ArchivedAt = signal.ArchivedAt.HasValue ? Iso(signal.ArchivedAt.Value) : null,
        DismissedAt = signal.DismissedAt.HasValue ? Iso(signal.DismissedAt.Value) : null,
        Sender = ToUserPublicDto(signal.SenderUser),
        Receiver = ToUserPublicDto(signal.ReceiverUser),
        Recipient = ToUserPublicDto(signal.ReceiverUser)
    };

    public static string ToPublicationContextApi(PostPublicationContext ctx) =>
        ctx == PostPublicationContext.Profile ? "profile" : "community";

    public static PostCommunityPreviewDto ToCommunityPreview(Community c)
    {
        var utcNow = DateTime.UtcNow;
        return new PostCommunityPreviewDto
        {
            Id = c.Id.ToString(),
            Slug = c.Slug,
            Name = c.Name,
            AvatarUrl = c.AvatarUrl,
            Category = c.Category,
            CommunityPlan = CommunityBillingMapper.ToEffectiveApiPlan(c.Subscription, utcNow)
        };
    }

    public static PostResponseDto ToPostDto(
        Post p,
        int likesCount,
        int commentsCount,
        int? viewerUserId,
        bool likedByCurrentUser,
        bool communityBoostActive = false,
        string? communityBoostEndsAt = null)
    {
        var ordered = p.Images.OrderBy(i => i.DisplayOrder).ToList();

        var imageUrls = ordered
            .Where(i => i.MediaKind != MediaKind.Video)
            .Select(i => i.Url)
            .ToList();
        if (imageUrls.Count == 0 && !string.IsNullOrWhiteSpace(p.ImageUrl))
            imageUrls = new List<string> { p.ImageUrl };

        var mediaAttachments = ordered
            .Select(i => new PostMediaAttachmentResponseDto
            {
                Url = i.Url,
                MediaType = MediaKindApi.ToApiString(i.MediaKind),
                MimeType = i.MimeType,
                DurationSeconds = i.DurationSeconds
            })
            .ToList();
        if (mediaAttachments.Count == 0 && imageUrls.Count > 0)
        {
            mediaAttachments = imageUrls
                .Select(u => new PostMediaAttachmentResponseDto
                {
                    Url = u,
                    MediaType = MediaKindApi.Image,
                    MimeType = null,
                    DurationSeconds = null
                })
                .ToList();
        }

        return new PostResponseDto
        {
            Id = p.Id.ToString(),
            PublicationContext = ToPublicationContextApi(p.PublicationContext),
            CommunityId = p.CommunityId?.ToString(),
            AuthorId = p.UserId.ToString(),
            Author = ToUserPublicDto(p.User),
            Title = p.Title,
            Content = p.Content,
            ImageUrl = imageUrls.Count > 0 ? imageUrls[0] : null,
            ImageUrls = imageUrls.Count > 0 ? imageUrls : null,
            MediaAttachments = mediaAttachments.Count > 0 ? mediaAttachments : null,
            Tags = p.Tags.Select(t => t.Tag).ToList(),
            CreatedAt = Iso(p.CreatedAt),
            UpdatedAt = p.UpdatedAt.HasValue ? Iso(p.UpdatedAt.Value) : null,
            DeletedAt = p.DeletedAt.HasValue ? Iso(p.DeletedAt.Value) : null,
            LikesCount = likesCount,
            CommentsCount = commentsCount,
            LikedByCurrentUser = likedByCurrentUser,
            Community = p.Community != null ? ToCommunityPreview(p.Community) : null,
            PinnedOnProfileAt = p.PinnedOnProfileAt.HasValue ? Iso(p.PinnedOnProfileAt.Value) : null,
            CommunityBoostActive = communityBoostActive,
            CommunityBoostEndsAt = communityBoostEndsAt
        };
    }

    public static CommentResponseDto ToCommentDto(Comment c, int postAuthorId, int? viewerUserId)
    {
        var hidden = c.HiddenByPostAuthorAt.HasValue;
        var viewerIsPostAuthor = viewerUserId == postAuthorId;
        var viewerIsCommentAuthor = viewerUserId == c.AuthorId;
        var showContent = !hidden || viewerIsPostAuthor || viewerIsCommentAuthor;
        string? mask = null;
        if (hidden && viewerUserId.HasValue && !viewerIsPostAuthor && !viewerIsCommentAuthor)
            mask = "hidden_by_post_author";

        return new CommentResponseDto
        {
            Id = c.Id.ToString(),
            PostId = c.PostId.ToString(),
            ParentCommentId = c.ParentCommentId?.ToString(),
            AuthorId = c.AuthorId.ToString(),
            Author = ToUserPublicDto(c.Author),
            Content = showContent ? c.Content : string.Empty,
            CreatedAt = Iso(c.CreatedAt),
            DeletedAt = c.DeletedAt.HasValue ? Iso(c.DeletedAt.Value) : null,
            HiddenByPostAuthorAt = c.HiddenByPostAuthorAt.HasValue ? Iso(c.HiddenByPostAuthorAt.Value) : null,
            ContentModerationMask = mask,
            PinnedOnPostAt = c.PinnedOnPostAt.HasValue ? Iso(c.PinnedOnPostAt.Value) : null
        };
    }

    public static CommunityResponseDto ToCommunityDto(Community c) => new()
    {
        Id = c.Id.ToString(),
        Slug = c.Slug,
        Name = c.Name,
        Description = c.Description,
        Category = c.Category,
        Tags = c.Tags.Select(t => t.Tag).ToList(),
        Rules = c.Rules,
        AvatarUrl = c.AvatarUrl,
        CoverUrl = c.CoverUrl,
        OwnerUserId = c.OwnerUserId.ToString(),
        Visibility = c.Visibility,
        MemberCount = c.MemberCount,
        Billing = CommunityBillingMapper.ToBillingStateDto(c.Subscription, DateTime.UtcNow)
    };

    public static UserProfileDto ToUserProfile(
        User u,
        bool? isFollowing = null,
        List<SocialLinkDto>? links = null,
        List<InterestItemResponseDto>? interests = null,
        int followersCount = 0,
        int followingCount = 0) => new()
    {
        Id = u.Id.ToString(),
        Name = u.DisplayName ?? u.Username,
        Username = u.Username,
        AvatarUrl = u.ProfilePic,
        Pronouns = u.Pronouns,
        BannerUrl = u.BannerPic,
        Bio = u.Bio ?? string.Empty,
        Location = u.Location,
        Role = u.Role,
        SocialLinks = links ?? new List<SocialLinkDto>(),
        Interests = interests ?? new List<InterestItemResponseDto>(),
        Suggestions = new List<object>(),
        IsFollowing = isFollowing,
        FollowersCount = followersCount,
        FollowingCount = followingCount,
        ShowProBadge = SubscriptionEntitlement.ShouldShowProBadge(u.Subscription, DateTime.UtcNow)
    };
}
