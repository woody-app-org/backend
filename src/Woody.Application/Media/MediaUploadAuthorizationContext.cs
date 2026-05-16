namespace Woody.Application.Media;

/// <summary>Dados de autorização e contexto para um upload multimédia.</summary>
public sealed record MediaUploadAuthorizationContext(
    int UserId,
    MediaUploadScope Scope,
    /// <summary><c>profile</c> ou <c>community</c> quando <see cref="Scope"/> é <see cref="MediaUploadScope.Post"/>.</summary>
    string? PublicationContext,
    int? CommunityId,
    /// <summary>Conversa quando <see cref="Scope"/> é <see cref="MediaUploadScope.Message"/>.</summary>
    int? ConversationId,
    /// <summary>Duração declarada pelo cliente (vídeo); validada contra o máximo do contexto.</summary>
    int? DeclaredDurationSeconds);
