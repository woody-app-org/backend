namespace Woody.Application.DTOs.Api;

public sealed class SendConversationMessageRequestDto
{
    public string? Body { get; set; }

    /// <summary>URLs públicas de imagens (legado; todas tratadas como <c>image</c>).</summary>
    public List<string>? AttachmentUrls { get; set; }

    /// <summary>Anexos com tipo semântico (imagem, vídeo, gif, sticker). Tem prioridade sobre <see cref="AttachmentUrls"/>.</summary>
    public List<MessageAttachmentItemRequestDto>? Attachments { get; set; }
}
