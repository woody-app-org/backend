using System.Text.Json;
using Microsoft.Extensions.Options;
using Woody.Application.Configuration;
using Woody.Application.DTOs;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Services;

public class VerificationService : IVerificationService
{
    private const int MagicBytesToRead = 24;

    // Restrição mais rígida que a mídia pública: apenas jpg/png para documentos de identidade
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    private static readonly Dictionary<string, string> ContentTypeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"]  = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"]  = "image/png"
        };

    private readonly IIdentityVerificationRepository _verifications;
    private readonly IUserRepository _users;
    private readonly IVerificationDocumentStorageProvider _storage;
    private readonly VerificationStorageOptions _storageOptions;

    public VerificationService(
        IIdentityVerificationRepository verifications,
        IUserRepository users,
        IVerificationDocumentStorageProvider storage,
        IOptions<VerificationStorageOptions> storageOptions)
    {
        _verifications = verifications;
        _users = users;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    public async Task<VerificationStatusDto> SubmitDocumentAsync(
        int userId,
        Stream fileContent,
        string originalFileName,
        string declaredContentType,
        long fileSizeBytes,
        bool consentGiven,
        CancellationToken cancellationToken = default)
    {
        var maxBytes = _storageOptions.MaxUploadBytes;

        // ── Validação de metadados ────────────────────────────────────────────────
        if (fileSizeBytes <= 0)
            throw new ArgumentException("Arquivo vazio.");

        if (fileSizeBytes > maxBytes)
            throw new ArgumentException($"O documento excede o tamanho máximo de {maxBytes / (1024 * 1024)} MB.");

        var fileName = Path.GetFileName(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != originalFileName)
            throw new ArgumentException("Nome de arquivo inválido.");

        if (UploadedImagePolicy.HasSuspiciousDoubleExtension(fileName))
            throw new ArgumentException("Extensão de arquivo inválida.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new ArgumentException("Apenas imagens JPG e PNG são aceitas como documento.");

        if (!ContentTypeByExtension.TryGetValue(extension, out var expectedContentType))
            throw new ArgumentException("Tipo de arquivo inválido.");

        if (!string.Equals(declaredContentType?.Trim(), expectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Tipo de arquivo declarado não corresponde à extensão.");

        // ── Buffer + validação de magic bytes ────────────────────────────────────
        await using var buffered = await BufferStreamAsync(fileContent, fileSizeBytes, maxBytes, cancellationToken);

        var headerLength = (int)Math.Min(MagicBytesToRead, buffered.Length);
        var header = new byte[headerLength];
        buffered.Position = 0;
        var read = await buffered.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        if (!UploadedImagePolicy.HasValidMagicBytes(header.AsSpan(0, read), extension))
            throw new ArgumentException("Assinatura do arquivo inválida. Verifique se o arquivo não está corrompido.");

        // ── Carregar entidades tracked ────────────────────────────────────────────
        var verification = await _verifications.GetByUserIdTrackedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Registro de verificação não encontrado.");

        var user = await _users.GetByIdTrackedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuária não encontrada.");

        if (verification.Status != VerificationStatus.PendingDocument
            && verification.Status != VerificationStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Não é possível enviar documento neste estado. Status atual: " + verification.Status);
        }

        var now = DateTime.UtcNow;

        // ── Deletar documento anterior se existir ─────────────────────────────────
        if (!string.IsNullOrEmpty(verification.DocumentStorageKey))
        {
            await _storage.DeleteAsync(verification.DocumentStorageKey, cancellationToken);
            // Não logar o storageKey anterior
        }

        // ── Salvar novo documento ─────────────────────────────────────────────────
        buffered.Position = 0;
        var storageKey = await _storage.SaveAsync(userId, buffered, extension, cancellationToken);

        // ── Atualizar IdentityVerification ────────────────────────────────────────
        var isResubmit = verification.Status == VerificationStatus.Rejected;
        if (isResubmit)
            verification.AttemptCount++;

        verification.DocumentStorageKey = storageKey;
        verification.DocumentSubmittedAt = now;
        verification.ConsentGivenAt = consentGiven ? now : verification.ConsentGivenAt;
        verification.Status = VerificationStatus.PendingReview;
        verification.RejectionReason = null;
        verification.UpdatedAt = now;
        verification.DecisionLog = AppendDecisionLogEvent(
            verification.DecisionLog,
            isResubmit ? "resubmitted" : "submitted",
            userId,
            now);

        // ── Atualizar cache no User ───────────────────────────────────────────────
        user.VerificationStatus = VerificationStatus.PendingReview;
        user.UpdatedAt = now;

        await _verifications.SaveChangesAsync(cancellationToken);

        return ToDto(verification);
    }

    public async Task<VerificationStatusDto?> GetStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var verification = await _verifications.GetByUserIdAsync(userId, cancellationToken);
        return verification == null ? null : ToDto(verification);
    }

    public async Task<VerificationStatusDto> DeleteDocumentAsync(int userId, CancellationToken cancellationToken = default)
    {
        var verification = await _verifications.GetByUserIdTrackedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Registro de verificação não encontrado.");

        var user = await _users.GetByIdTrackedAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuária não encontrada.");

        if (verification.Status != VerificationStatus.PendingReview
            && verification.Status != VerificationStatus.Rejected)
        {
            throw new InvalidOperationException(
                "Remoção do documento não permitida neste estado. Status atual: " + verification.Status);
        }

        var now = DateTime.UtcNow;

        // Deletar do storage
        if (!string.IsNullOrEmpty(verification.DocumentStorageKey))
        {
            await _storage.DeleteAsync(verification.DocumentStorageKey, cancellationToken);
            verification.DocumentDeletedAt = now;
        }

        verification.DocumentStorageKey = null;
        verification.DocumentSubmittedAt = null;
        verification.Status = VerificationStatus.PendingDocument;
        verification.UpdatedAt = now;
        verification.DecisionLog = AppendDecisionLogEvent(
            verification.DecisionLog,
            "document_removed",
            userId,
            now);

        user.VerificationStatus = VerificationStatus.PendingDocument;
        user.UpdatedAt = now;

        await _verifications.SaveChangesAsync(cancellationToken);

        return ToDto(verification);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static VerificationStatusDto ToDto(IdentityVerification v) => new()
    {
        Status = v.Status.ToString(),
        RejectionReason = v.Status == VerificationStatus.Rejected ? v.RejectionReason : null,
        DocumentSubmittedAt = v.DocumentSubmittedAt.HasValue
            ? v.DocumentSubmittedAt.Value.ToUniversalTime().ToString("o")
            : null,
        ReviewedAt = v.ReviewedAt.HasValue
            ? v.ReviewedAt.Value.ToUniversalTime().ToString("o")
            : null,
        AttemptCount = v.AttemptCount
    };

    private static string AppendDecisionLogEvent(
        string? existingLog,
        string action,
        int actorUserId,
        DateTime at)
    {
        var events = new List<object>();

        if (!string.IsNullOrWhiteSpace(existingLog))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<JsonElement>>(existingLog);
                if (parsed != null)
                    events.AddRange(parsed.Cast<object>());
            }
            catch
            {
                // Log corrompido: inicia novo
            }
        }

        events.Add(new
        {
            action,
            by = actorUserId,
            at = at.ToUniversalTime().ToString("o")
        });

        return JsonSerializer.Serialize(events);
    }

    private static async Task<MemoryStream> BufferStreamAsync(
        Stream content,
        long declaredSizeBytes,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (declaredSizeBytes > maxBytes)
            throw new ArgumentException("Arquivo inválido.");

        var capacity = (int)Math.Min(declaredSizeBytes, int.MaxValue);
        var buffered = new MemoryStream(capacity: Math.Max(capacity, 256));
        var buffer = new byte[64 * 1024];
        long totalRead = 0;

        while (totalRead < declaredSizeBytes)
        {
            var toRead = (int)Math.Min(buffer.Length, declaredSizeBytes - totalRead);
            var read = await content.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
            if (read == 0)
                break;
            totalRead += read;
            if (totalRead > maxBytes)
                throw new ArgumentException("Arquivo inválido.");
            await buffered.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (buffered.Length != declaredSizeBytes)
            throw new ArgumentException("Tamanho do arquivo não corresponde ao declarado.");

        return buffered;
    }
}
