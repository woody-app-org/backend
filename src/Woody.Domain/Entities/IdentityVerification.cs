using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Registro de verificação de identidade por documentos.
/// Relação 1:1 com <see cref="User"/> — criada automaticamente no registo.
/// O <see cref="DocumentStorageKey"/> aponta para bucket privado e nunca deve ser exposto em responses públicas.
/// </summary>
public class IdentityVerification
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public VerificationStatus Status { get; set; } = VerificationStatus.PendingDocument;

    /// <summary>Chave no bucket de armazenamento privado. Nunca retornar em responses públicas.</summary>
    public string? DocumentStorageKey { get; set; }

    public DateTime? DocumentSubmittedAt { get; set; }

    /// <summary>Admin interno (SuperAdmin) que tomou a decisão.</summary>
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Obrigatório quando Status = Rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Número de tentativas de envio de documento (permite reenvio após recusa).</summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>Momento em que a utilizadora deu consentimento explícito ao enviar o documento.</summary>
    public DateTime? ConsentGivenAt { get; set; }

    /// <summary>Preenchido quando o arquivo do documento é deletado do storage (retenção limitada).</summary>
    public DateTime? DocumentDeletedAt { get; set; }

    /// <summary>
    /// Log de auditoria em JSON. Registra ações e timestamps sem incluir storageKey ou URLs do documento.
    /// Exemplo: [{"action":"submitted","at":"..."},{"action":"approved","by":5,"at":"..."}]
    /// </summary>
    public string? DecisionLog { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
