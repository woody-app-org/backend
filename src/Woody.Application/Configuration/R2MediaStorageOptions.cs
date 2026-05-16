namespace Woody.Application.Configuration;

/// <summary>Configuração para armazenamento S3-compatible (ex.: Cloudflare R2).</summary>
public sealed class R2MediaStorageOptions
{
    /// <summary>Account ID R2 (subdomínio do endpoint).</summary>
    public string AccountId { get; set; } = "";

    public string AccessKeyId { get; set; } = "";

    public string SecretAccessKey { get; set; } = "";

    public string Bucket { get; set; } = "";

    /// <summary>Base pública (CDN / r2.dev), sem barra final. Opcional: URLs caem no proxy da API.</summary>
    public string PublicBaseUrl { get; set; } = "";

    /// <summary>Endpoint custom; vazio usa <c>https://{AccountId}.r2.cloudflarestorage.com</c>.</summary>
    public string? ServiceUrl { get; set; }

    /// <summary>Região exigida pelo SDK (R2 aceita valor fixo).</summary>
    public string Region { get; set; } = "auto";
}
