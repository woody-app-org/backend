namespace Woody.Application.DTOs.Api;

public sealed class SharePostToConversationResponseDto
{
    public int ConversationId { get; set; }
    public MessageResponseDto Message { get; set; } = null!;
}
