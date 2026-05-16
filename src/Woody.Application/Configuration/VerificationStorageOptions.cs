namespace Woody.Application.Configuration;

public class VerificationStorageOptions
{
    /// <summary><c>Local</c> (disco privado) ou <c>S3</c> (bucket R2/S3 privado).</summary>
    public string Driver { get; set; } = "Local";

    /// <summary>Caminho raiz para armazenamento local. Deve estar fora do wwwroot.</summary>
    public string RootPath { get; set; } = "App_Data/verification";

    /// <summary>Tamanho máximo permitido para o documento de identidade em bytes.</summary>
    public long MaxUploadBytes { get; set; } = 8 * 1024 * 1024; // 8 MB

    /// <summary>Expiração das presigned URLs (quando usar S3/R2). Não aplicável ao provider local.</summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 5;
}
