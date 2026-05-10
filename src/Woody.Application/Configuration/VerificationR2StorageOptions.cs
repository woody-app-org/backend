namespace Woody.Application.Configuration;

/// <summary>
/// Configuração R2/S3-compatible para armazenamento <b>privado</b> de documentos de verificação de identidade.
/// Este bucket nunca deve ter acesso público habilitado; documentos só são acessíveis via streaming autenticado.
/// Bind automático: variáveis de ambiente <c>VerificationR2__*</c> (duplo underscore).
/// </summary>
public sealed class VerificationR2StorageOptions
{
    /// <summary>Account ID R2 (usado para construir o endpoint <c>https://{AccountId}.r2.cloudflarestorage.com</c>).</summary>
    public string AccountId { get; set; } = "";

    public string AccessKeyId { get; set; } = "";

    public string SecretAccessKey { get; set; } = "";

    public string Bucket { get; set; } = "";

    /// <summary>Endpoint customizado; vazio usa <c>https://{AccountId}.r2.cloudflarestorage.com</c>.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Região exigida pelo SDK AWS. R2 aceita <c>auto</c>.</summary>
    public string Region { get; set; } = "auto";

    /// <summary>
    /// Expiração de presigned URLs de leitura (minutos). Reservado para uso futuro —
    /// o fluxo atual serve documentos via streaming na API, sem URL direta.
    /// </summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 5;
}
