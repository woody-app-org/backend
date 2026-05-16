namespace Woody.Domain.Media;

public sealed record UploadedVideoValidationResult(
    bool IsValid,
    string? Error,
    string? Extension = null,
    string? ContentType = null);

/// <summary>Validação de vídeo (upload directo). Duração exacta exige pipeline futuro (ex.: ffprobe).</summary>
public static class UploadedVideoPolicy
{
    /// <summary>Limite genérico legado quando não há contexto (evitar uso em novos fluxos).</summary>
    public const long DefaultMaxSizeBytes = 50 * 1024 * 1024;

    public const int DefaultMaxDeclaredDurationSeconds = 300;

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".mp4"] = "video/mp4",
            [".m4v"] = "video/mp4",
            [".mov"] = "video/quicktime",
            [".webm"] = "video/webm"
        };

    private static readonly HashSet<string> DangerousExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".svg", ".html", ".htm", ".js", ".mjs", ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh",
            ".php", ".aspx", ".jsp", ".jar", ".msi", ".scr", ".com"
        };

    public static UploadedVideoValidationResult ValidateMetadata(
        string? originalFileName,
        string? contentType,
        long sizeBytes,
        long maxSizeBytes = DefaultMaxSizeBytes,
        int? maxDeclaredDurationSeconds = null,
        int? clientDeclaredDurationSeconds = null)
    {
        if (sizeBytes <= 0)
            return Invalid("Arquivo vazio.");

        if (sizeBytes > maxSizeBytes)
            return Invalid($"Arquivo excede o tamanho máximo de {maxSizeBytes} bytes.");

        var fileName = Path.GetFileName(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != originalFileName)
            return Invalid("Nome de arquivo inválido.");

        if (UploadedImagePolicy.HasSuspiciousDoubleExtension(fileName))
            return Invalid("Extensão de arquivo inválida.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
            return Invalid("Extensão de vídeo inválida (permitido: mp4, mov, webm).");

        if (DangerousExtensions.Contains(extension))
            return Invalid("Extensão de arquivo inválida.");

        if (!string.Equals(contentType?.Trim(), expectedContentType, StringComparison.OrdinalIgnoreCase))
            return Invalid("Tipo de arquivo inválido.");

        if (maxDeclaredDurationSeconds is int cap && clientDeclaredDurationSeconds is int d)
        {
            if (d < 0 || d > cap)
                return Invalid($"Duração declarada inválida (máximo {cap} s).");
        }

        return new UploadedVideoValidationResult(true, null, extension, expectedContentType);
    }

    public static bool HasValidMagicBytes(ReadOnlySpan<byte> bytes, string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" or ".m4v" or ".mov" => bytes.Length >= 12
                                         && bytes[4] == (byte)'f'
                                         && bytes[5] == (byte)'t'
                                         && bytes[6] == (byte)'y'
                                         && bytes[7] == (byte)'p',
            ".webm" => bytes.Length >= 4
                       && bytes[0] == 0x1A
                       && bytes[1] == 0x45
                       && bytes[2] == 0xDF
                       && bytes[3] == 0xA3,
            _ => false
        };
    }

    public static string? GetContentTypeForStorageKey(string storageKey)
    {
        var extension = Path.GetExtension(storageKey);
        return ContentTypesByExtension.TryGetValue(extension, out var contentType) ? contentType : null;
    }

    private static UploadedVideoValidationResult Invalid(string error) =>
        new(false, error);
}
