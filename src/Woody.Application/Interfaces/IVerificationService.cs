using Woody.Application.DTOs;

namespace Woody.Application.Interfaces;

public interface IVerificationService
{
    /// <summary>Processa o envio do documento de identidade. Permitido quando PendingDocument ou Rejected.</summary>
    Task<VerificationStatusDto> SubmitDocumentAsync(
        int userId,
        Stream fileContent,
        string originalFileName,
        string declaredContentType,
        long fileSizeBytes,
        bool consentGiven,
        CancellationToken cancellationToken = default);

    /// <summary>Retorna o status atual sem expor o storageKey ou URL do documento.</summary>
    Task<VerificationStatusDto?> GetStatusAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Remove o documento e volta status para PendingDocument. Permitido quando PendingReview ou Rejected.</summary>
    Task<VerificationStatusDto> DeleteDocumentAsync(int userId, CancellationToken cancellationToken = default);
}
