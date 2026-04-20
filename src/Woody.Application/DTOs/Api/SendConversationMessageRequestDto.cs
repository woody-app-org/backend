namespace Woody.Application.DTOs.Api;

public sealed class SendConversationMessageRequestDto
{
    public string? Body { get; set; }

    /// <summary>URLs públicas de imagens (upload tratado à parte; mesmo padrão conceptual dos posts).</summary>
    public List<string>? AttachmentUrls { get; set; }
}
