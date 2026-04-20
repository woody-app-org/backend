namespace Woody.Application.DTOs.Api;

public sealed class StartConversationRequestDto
{
    /// <summary>Identificador da outra utilizadora com quem se pretende conversar.</summary>
    public int OtherUserId { get; set; }
}
