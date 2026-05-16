namespace Woody.Domain.Media;

/// <summary>
/// Metadados por chave de ficheiro no storage local (preparado para o mesmo contrato em R2/S3).
/// </summary>
public static class AttachmentStorageCatalog
{
    public static string? GetContentTypeForStorageKey(string storageKey) =>
        UploadedImagePolicy.GetContentTypeForStorageKey(storageKey)
        ?? UploadedVideoPolicy.GetContentTypeForStorageKey(storageKey);

    public static bool IsImageStorageKey(string storageKey)
    {
        var ct = GetContentTypeForStorageKey(storageKey);
        return ct != null && ct.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsVideoStorageKey(string storageKey)
    {
        var ct = GetContentTypeForStorageKey(storageKey);
        return ct != null && ct.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
    }
}
