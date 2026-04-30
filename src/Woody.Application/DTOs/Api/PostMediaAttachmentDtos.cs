namespace Woody.Application.DTOs.Api;

public sealed class PostMediaAttachmentRequestDto
{
    public string Url { get; set; } = null!;
    /// <summary><c>image</c> | <c>video</c> | <c>gif</c> | <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;
    public int? DurationSeconds { get; set; }
    public string? Provider { get; set; }
    public string? ExternalId { get; set; }

    /// <summary>Chave interna (ex.: <c>posts/1/…</c>) devolvida pelo upload; deve coincidir com a URL local.</summary>
    public string? StorageKey { get; set; }

    /// <summary>MIME do ficheiro; para URLs locais validado contra a extensão da <see cref="StorageKey"/>.</summary>
    public string? MimeType { get; set; }

    /// <summary>Tamanho em bytes (opcional; só aceite com URL de media Woody).</summary>
    public long? FileSize { get; set; }
}
