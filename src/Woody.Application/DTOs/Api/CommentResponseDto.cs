namespace Woody.Application.DTOs.Api;

public class CommentResponseDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? ParentCommentId { get; set; }
    public string AuthorId { get; set; } = null!;
    public UserPublicDto Author { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string CreatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }
    public string? HiddenByPostAuthorAt { get; set; }
    public string? ContentModerationMask { get; set; }
}
