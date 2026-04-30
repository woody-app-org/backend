namespace Woody.Application.DTOs.Api;

/// <summary>Resposta genérica de anexo multimédia (posts e mensagens).</summary>
public sealed class MediaAttachmentResponseDto
{
    public int Id { get; set; }

    /// <summary><c>post</c> ou <c>message</c>.</summary>
    public string OwnerType { get; set; } = null!;

    public string OwnerId { get; set; } = null!;

    /// <summary><c>image</c> | <c>video</c> | <c>gif</c> | <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;

    public string Url { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
    public string? MimeType { get; set; }
    public long? FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? DurationMs { get; set; }

    /// <summary>Compatível com clientes que ainda leem duração em segundos.</summary>
    public int? DurationSeconds { get; set; }

    public string? Provider { get; set; }
    public string? ExternalId { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}
