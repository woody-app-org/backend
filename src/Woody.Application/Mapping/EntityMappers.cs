using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;

namespace Woody.Application.Mapping;

public static class EntityMappers
{
    public static string Iso(DateTime dt) => dt.ToUniversalTime().ToString("o");

    public static UserPublicDto ToUserPublicDto(User u) => new()
    {
        Id = u.Id.ToString(),
        Name = u.DisplayName ?? u.Username,
        Username = u.Username,
        AvatarUrl = u.ProfilePic,
        Bio = u.Bio,
        Pronouns = u.Pronouns
    };

    public static PostCommunityPreviewDto ToCommunityPreview(Community c) => new()
    {
        Id = c.Id.ToString(),
        Slug = c.Slug,
        Name = c.Name,
        AvatarUrl = c.AvatarUrl,
        Category = c.Category
    };

    public static PostResponseDto ToPostDto(
        Post p,
        int likesCount,
        int commentsCount,
        int? viewerUserId,
        bool likedByCurrentUser)
    {
        return new PostResponseDto
        {
            Id = p.Id.ToString(),
            CommunityId = p.CommunityId.ToString(),
            AuthorId = p.UserId.ToString(),
            Author = ToUserPublicDto(p.User),
            Title = p.Title,
            Content = p.Content,
            ImageUrl = p.ImageUrl,
            Tags = p.Tags.Select(t => t.Tag).ToList(),
            CreatedAt = Iso(p.CreatedAt),
            UpdatedAt = p.UpdatedAt.HasValue ? Iso(p.UpdatedAt.Value) : null,
            DeletedAt = p.DeletedAt.HasValue ? Iso(p.DeletedAt.Value) : null,
            LikesCount = likesCount,
            CommentsCount = commentsCount,
            LikedByCurrentUser = likedByCurrentUser,
            Community = ToCommunityPreview(p.Community)
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
            ContentModerationMask = mask
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
        MemberCount = c.MemberCount
    };

    public static UserProfileDto ToUserProfile(
        User u,
        bool? isFollowing = null,
        List<SocialLinkDto>? links = null,
        List<InterestItemResponseDto>? interests = null) => new()
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
        IsFollowing = isFollowing
    };
}
