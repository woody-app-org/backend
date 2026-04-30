using Woody.Application.DTOs.Api;
using Woody.Domain.Entities;
using Woody.Domain.Entities.Enum;
using Woody.Domain.Media;

namespace Woody.Application.Mapping;

public static class MediaAttachmentDtoMapper
{
    public static MediaAttachmentResponseDto ToResponseDto(MediaAttachment a)
    {
        var owner = a.OwnerType;
        return new MediaAttachmentResponseDto
        {
            Id = a.Id,
            OwnerType = MediaOwnerTypeApi.ToApiString(owner),
            OwnerId = a.OwnerId.ToString(),
            MediaType = MediaKindApi.ToApiString(a.MediaKind),
            Url = a.Url,
            ThumbnailUrl = a.ThumbnailUrl,
            MimeType = a.MimeType,
            FileSize = a.FileSize,
            Width = a.Width,
            Height = a.Height,
            DurationMs = a.DurationMs,
            DurationSeconds = a.DurationMs.HasValue ? (a.DurationMs.Value + 999) / 1000 : null,
            Provider = a.Provider,
            ExternalId = a.ExternalId,
            StorageKey = a.StorageKey,
            DisplayOrder = a.DisplayOrder,
            CreatedAt = a.CreatedAt
        };
    }

    /// <summary>Anexo sintético a partir só de URL (legado <see cref="Post.ImageUrl"/> / galeria sem linha dedicada).</summary>
    public static MediaAttachmentResponseDto FromLegacyUrl(
        MediaOwnerType ownerType,
        int ownerId,
        string url,
        MediaKind kind,
        int displayOrder,
        DateTime createdAt)
    {
        return new MediaAttachmentResponseDto
        {
            Id = 0,
            OwnerType = MediaOwnerTypeApi.ToApiString(ownerType),
            OwnerId = ownerId.ToString(),
            MediaType = MediaKindApi.ToApiString(kind),
            Url = url,
            ThumbnailUrl = null,
            MimeType = null,
            FileSize = null,
            Width = null,
            Height = null,
            DurationMs = null,
            DurationSeconds = null,
            Provider = null,
            ExternalId = null,
            StorageKey = null,
            DisplayOrder = displayOrder,
            CreatedAt = createdAt
        };
    }
}
