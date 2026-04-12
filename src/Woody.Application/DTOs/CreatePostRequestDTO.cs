namespace Woody.Application.DTOs;

public class CreatePostRequestDTO
{
    /// <summary>Omissão ou vazio = publicação no perfil da autora; preenchido = publicação na comunidade indicada.</summary>
    public string? CommunityId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    /// <summary>Uma única imagem (legado). Se <see cref="ImageUrls"/> vier preenchido, ele tem prioridade.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Várias URLs públicas (ex.: após upload para CDN/storage). Ordem preservada.</summary>
    public List<string>? ImageUrls { get; set; }
    public List<string>? Tags { get; set; }
}
