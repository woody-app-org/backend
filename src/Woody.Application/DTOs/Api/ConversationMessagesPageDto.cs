namespace Woody.Application.DTOs.Api;

public sealed class ConversationMessagesPageDto
{
    public IReadOnlyList<MessageResponseDto> Items { get; set; } = Array.Empty<MessageResponseDto>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
