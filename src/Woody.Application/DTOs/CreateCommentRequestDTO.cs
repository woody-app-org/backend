namespace Woody.Application.DTOs;

public class CreateCommentRequestDTO
{
    /// <summary>Texto opcional quando há GIF válido; obrigatório sem GIF.</summary>
    public string? Content { get; set; }

    public string? ParentCommentId { get; set; }

    /// <summary>URL https do GIF (ficheiro <c>.gif</c>); obrigatório se qualquer campo GIF for enviado.</summary>
    public string? GifUrl { get; set; }

    public string? GifThumbnailUrl { get; set; }
    public string? GifProvider { get; set; }
    public string? GifExternalId { get; set; }
    public string? GifTitle { get; set; }
}
