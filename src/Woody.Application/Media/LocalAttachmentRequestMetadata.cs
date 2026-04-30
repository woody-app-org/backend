using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Media;

/// <summary>
/// Valida metadados opcionais do cliente (<c>storageKey</c>, <c>mimeType</c>, <c>fileSize</c>) contra URLs de media Woody.
/// </summary>
public static class LocalAttachmentRequestMetadata
{
    public static bool TryResolve(
        MediaKind kind,
        string normalizedUrl,
        string? declaredStorageKey,
        string? declaredMimeType,
        long? declaredFileSize,
        long maxVideoBytes,
        long maxImageBytes,
        out string? storageKey,
        out string? resolvedMimeType,
        out long? resolvedFileSize,
        out string? error)
    {
        storageKey = null;
        resolvedMimeType = null;
        resolvedFileSize = null;
        error = null;

        string? derived = null;
        if (kind == MediaKind.Video)
        {
            if (LocalMediaUrlParser.TryGetVideoStorageKeyFromLocalUrl(normalizedUrl, out var vk))
                derived = vk;
        }
        else
        {
            if (LocalMediaUrlParser.TryGetImageStorageKeyFromLocalUrl(normalizedUrl, out var ik))
                derived = ik;
        }

        var declKey = string.IsNullOrWhiteSpace(declaredStorageKey) ? null : declaredStorageKey.Trim();
        if (declKey != null && derived == null)
        {
            error = "storageKey só é permitido para URLs de media da plataforma (upload Woody).";
            return false;
        }

        if (declKey != null && derived != null && !string.Equals(declKey, derived, StringComparison.Ordinal))
        {
            error = "storageKey não coincide com a URL do anexo.";
            return false;
        }

        storageKey = derived;

        if (storageKey != null)
        {
            var expectedMime = AttachmentStorageCatalog.GetContentTypeForStorageKey(storageKey);
            if (expectedMime == null)
            {
                error = "Tipo de anexo inválido.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(declaredMimeType)
                && !string.Equals(declaredMimeType.Trim(), expectedMime, StringComparison.OrdinalIgnoreCase))
            {
                error = "mimeType não corresponde ao ficheiro indicado.";
                return false;
            }

            resolvedMimeType = expectedMime;

            var maxBytes = kind == MediaKind.Video ? maxVideoBytes : maxImageBytes;
            if (declaredFileSize is long sz)
            {
                if (sz <= 0 || sz > maxBytes)
                {
                    error = $"fileSize inválido (máximo {maxBytes} bytes para este tipo).";
                    return false;
                }

                resolvedFileSize = sz;
            }
        }
        else
        {
            if (declaredFileSize != null)
            {
                error = "fileSize só pode ser enviado com URL de upload Woody.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(declaredMimeType))
            {
                error = "mimeType extra só é permitido com URL de upload Woody.";
                return false;
            }
        }

        return true;
    }
}
