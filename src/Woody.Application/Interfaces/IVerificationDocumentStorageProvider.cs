namespace Woody.Application.Interfaces;

/// <summary>
/// Abstração de storage privado para documentos de verificação de identidade.
/// O arquivo nunca deve ser exposto por URL pública; acesso apenas via endpoint autenticado de admin.
/// </summary>
public interface IVerificationDocumentStorageProvider
{
    /// <summary>
    /// Persiste o stream no storage privado e retorna a storageKey gerada.
    /// Formato: <c>verif/{userId}/{guid}.{ext}</c>
    /// </summary>
    Task<string> SaveAsync(
        int userId,
        Stream content,
        string normalizedExtension,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Abre o arquivo para leitura. Retorna null se não encontrado.
    /// Nunca deve construir uma URL pública.
    /// </summary>
    Task<VerificationDocumentReadResult?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>Deleta o arquivo do storage. Silencioso se não existir.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>Verifica se a storageKey tem o formato esperado para documentos de verificação.</summary>
    bool IsValidStorageKey(string storageKey);
}

public sealed record VerificationDocumentReadResult(
    Stream Content,
    string ContentType,
    long SizeBytes);
