namespace Woody.Application.DTOs.Api;

public sealed class MessageAttachmentResponseDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public string? ContentType { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
