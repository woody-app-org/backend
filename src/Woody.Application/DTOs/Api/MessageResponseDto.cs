namespace Woody.Application.DTOs.Api;

public sealed class MessageResponseDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }

    public MessageAuthorResponseDto Sender { get; set; } = null!;

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }

    public IReadOnlyList<MediaAttachmentResponseDto> Attachments { get; set; } = Array.Empty<MediaAttachmentResponseDto>();
}
