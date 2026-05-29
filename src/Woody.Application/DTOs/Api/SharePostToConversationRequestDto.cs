namespace Woody.Application.DTOs.Api;

public sealed class SharePostToConversationRequestDto
{
    /// <summary>Destinatária quando a conversa ainda não existe ou para iniciar/obter par.</summary>
    public int? RecipientUserId { get; set; }

    /// <summary>Conversa existente (alternativa a <see cref="RecipientUserId"/>).</summary>
    public int? ConversationId { get; set; }

    /// <summary>Texto opcional acompanhando o post partilhado.</summary>
    public string? Message { get; set; }
}
