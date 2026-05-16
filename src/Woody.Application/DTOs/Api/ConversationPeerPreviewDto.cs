namespace Woody.Application.DTOs.Api;

public sealed class ConversationPeerPreviewDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? ProfilePic { get; set; }
}
