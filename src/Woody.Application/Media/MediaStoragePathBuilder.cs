namespace Woody.Application.Media;

/// <summary>Prefixo de object key para uploads (posts/… ou messages/…).</summary>
public sealed record MediaStorageWriteContext(string ObjectKeyPrefix);

/// <summary>Centraliza regras de path de blobs multimédia.</summary>
public static class MediaStoragePathBuilder
{
    /// <summary>
    /// Gera o prefixo <c>posts/{userId}/</c> ou <c>messages/{conversationId}/</c>.
    /// </summary>
    public static MediaStorageWriteContext FromAuthorization(MediaUploadAuthorizationContext authorization)
    {
        return authorization.Scope switch
        {
            MediaUploadScope.Post when authorization.UserId > 0 =>
                new MediaStorageWriteContext($"posts/{authorization.UserId}/"),
            MediaUploadScope.Message when authorization.ConversationId is int cid && cid > 0 =>
                new MediaStorageWriteContext($"messages/{cid}/"),
            MediaUploadScope.Post => throw new ArgumentException("userId inválido para prefixo de armazenamento."),
            MediaUploadScope.Message => throw new ArgumentException("conversationId inválido para prefixo de armazenamento."),
            _ => throw new ArgumentException("Escopo de upload inválido para armazenamento.")
        };
    }
}
