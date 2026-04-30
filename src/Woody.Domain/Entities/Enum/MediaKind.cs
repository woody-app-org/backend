namespace Woody.Domain.Entities.Enum;

/// <summary>
/// Tipo semântico do anexo (não confundir com MIME: vídeo e imagem têm pipelines distintos).
/// </summary>
public enum MediaKind
{
    Image = 0,
    Video = 1,
    Gif = 2,
    Sticker = 3
}
