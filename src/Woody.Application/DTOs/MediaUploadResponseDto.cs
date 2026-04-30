using Woody.Domain.Media;

namespace Woody.Application.DTOs;

public class MediaUploadResponseDto
{
    public string Url { get; set; } = null!;
    public string StorageKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }

    /// <summary>Tipo semântico inferido no servidor (<c>image</c>, <c>video</c>, <c>gif</c>).</summary>
    public string MediaKind { get; set; } = MediaKindApi.Image;

    /// <summary>Preenchido no upload de vídeo quando o cliente envia <c>durationSeconds</c> (ms derivado).</summary>
    public int? DurationMs { get; set; }

    public int? DurationSeconds { get; set; }
}
