namespace Woody.Application.DTOs;

public class CreateCommentRequestDTO
{
    public string Content { get; set; } = null!;
    public string? ParentCommentId { get; set; }
}
