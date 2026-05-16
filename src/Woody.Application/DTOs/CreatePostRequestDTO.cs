using Woody.Application.DTOs.Api;

namespace Woody.Application.DTOs;

public class CreatePostRequestDTO
{
    /// <summary>
    /// <c>profile</c> | <c>community</c>. Quando omitido, o servidor infere: sem <see cref="CommunityId"/> = perfil; com id = comunidade (legado).
    /// </summary>
    public string? PublicationContext { get; set; }

    /// <summary>Obrigatório quando o contexto é comunidade; omitido ou vazio para perfil.</summary>
    public string? CommunityId { get; set; }
    public string Content { get; set; } = null!;
    /// <summary>Uma única imagem (legado). Se <see cref="ImageUrls"/> vier preenchido, ele tem prioridade.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Várias URLs públicas (ex.: após upload para CDN/storage). Ordem preservada.</summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// Anexos tipados (imagem, vídeo, gif, sticker). Quando preenchido, tem prioridade sobre <see cref="ImageUrls"/> / <see cref="ImageUrl"/>.
    /// </summary>
    public List<PostMediaAttachmentRequestDto>? MediaAttachments { get; set; }

    public List<string>? Tags { get; set; }
}
