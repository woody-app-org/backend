namespace Woody.Application.DTOs.Api;

public class PostResponseDto
{
    public string Id { get; set; } = null!;
    public string CommunityId { get; set; } = null!;
    public string AuthorId { get; set; } = null!;
    public UserPublicDto Author { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    public List<string>? Tags { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string? UpdatedAt { get; set; }
    public string? DeletedAt { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public PostCommunityPreviewDto? Community { get; set; }
}
