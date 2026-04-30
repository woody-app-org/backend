namespace Woody.Application.DTOs.Api;

public sealed class PostMediaAttachmentRequestDto
{
    public string Url { get; set; } = null!;
    /// <summary><c>image</c> | <c>video</c> | <c>gif</c> | <c>sticker</c></summary>
    public string MediaType { get; set; } = null!;
    public int? DurationSeconds { get; set; }
}

public sealed class PostMediaAttachmentResponseDto
{
    public string Url { get; set; } = null!;
    public string MediaType { get; set; } = null!;
    public string? MimeType { get; set; }
    public int? DurationSeconds { get; set; }
}
