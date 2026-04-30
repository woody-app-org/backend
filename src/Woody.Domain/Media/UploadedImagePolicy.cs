namespace Woody.Domain.Media;

public sealed record UploadedImageValidationResult(
    bool IsValid,
    string? Error,
    string? Extension = null,
    string? ContentType = null);

public static class UploadedImagePolicy
{
    public const long DefaultMaxSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif"
        };

    private static readonly HashSet<string> DangerousExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".svg", ".html", ".htm", ".js", ".mjs", ".exe", ".dll", ".bat", ".cmd", ".ps1", ".sh",
            ".php", ".aspx", ".jsp", ".jar", ".msi", ".scr", ".com"
        };

    public static UploadedImageValidationResult ValidateMetadata(
        string? originalFileName,
        string? contentType,
        long sizeBytes,
        long maxSizeBytes = DefaultMaxSizeBytes)
    {
        if (sizeBytes <= 0)
            return Invalid("Arquivo vazio.");

        if (sizeBytes > maxSizeBytes)
            return Invalid($"Arquivo excede o tamanho máximo de {maxSizeBytes} bytes.");

        var fileName = Path.GetFileName(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(fileName) || fileName != originalFileName)
            return Invalid("Nome de arquivo inválido.");

        if (HasSuspiciousDoubleExtension(fileName))
            return Invalid("Extensão de arquivo inválida.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!ContentTypesByExtension.TryGetValue(extension, out var expectedContentType))
            return Invalid("Extensão de arquivo inválida.");

        if (DangerousExtensions.Contains(extension))
            return Invalid("Extensão de arquivo inválida.");

        if (!string.Equals(contentType?.Trim(), expectedContentType, StringComparison.OrdinalIgnoreCase))
            return Invalid("Tipo de arquivo inválido.");

        return new UploadedImageValidationResult(true, null, extension, expectedContentType);
    }

    public static bool HasValidMagicBytes(ReadOnlySpan<byte> bytes, string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => bytes.Length >= 3
                                  && bytes[0] == 0xFF
                                  && bytes[1] == 0xD8
                                  && bytes[2] == 0xFF,
            ".png" => bytes.Length >= 8
                      && bytes[0] == 0x89
                      && bytes[1] == 0x50
                      && bytes[2] == 0x4E
                      && bytes[3] == 0x47
                      && bytes[4] == 0x0D
                      && bytes[5] == 0x0A
                      && bytes[6] == 0x1A
                      && bytes[7] == 0x0A,
            ".webp" => bytes.Length >= 12
                       && bytes[0] == 0x52
                       && bytes[1] == 0x49
                       && bytes[2] == 0x46
                       && bytes[3] == 0x46
                       && bytes[8] == 0x57
                       && bytes[9] == 0x45
                       && bytes[10] == 0x42
                       && bytes[11] == 0x50,
            ".gif" => bytes.Length >= 6
                      && bytes[0] == 0x47
                      && bytes[1] == 0x49
                      && bytes[2] == 0x46
                      && (bytes[3] == 0x38 && (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61),
            _ => false
        };
    }

    public static string? GetContentTypeForStorageKey(string storageKey)
    {
        var extension = Path.GetExtension(storageKey);
        return ContentTypesByExtension.TryGetValue(extension, out var contentType) ? contentType : null;
    }

    public static bool HasSuspiciousDoubleExtension(string fileName)
    {
        var nameWithoutFinalExtension = Path.GetFileNameWithoutExtension(fileName);
        var previousExtension = Path.GetExtension(nameWithoutFinalExtension);
        return !string.IsNullOrEmpty(previousExtension);
    }

    private static UploadedImageValidationResult Invalid(string error) =>
        new(false, error);
}
