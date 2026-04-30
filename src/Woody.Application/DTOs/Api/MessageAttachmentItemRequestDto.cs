namespace Woody.Application.DTOs.Api;

public sealed class MessageAttachmentItemRequestDto
{
    public string Url { get; set; } = null!;
    /// <summary><c>image</c> | <c>video</c> | <c>gif</c> | <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;
    /// <summary>Pré-visualização estática (HTTPS); opcional para GIF/sticker externos.</summary>
    public string? ThumbnailUrl { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Provider { get; set; }
    public string? ExternalId { get; set; }

    /// <summary>Chave interna quando a URL é media Woody; deve coincidir com a URL.</summary>
    public string? StorageKey { get; set; }

    public string? MimeType { get; set; }

    public long? FileSize { get; set; }
}
