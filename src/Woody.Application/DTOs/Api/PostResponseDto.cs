namespace Woody.Application.DTOs.Api;

public class PostResponseDto
{
    public string Id { get; set; } = null!;
    /// <summary><c>profile</c> ou <c>community</c> — discrimina feeds e UI.</summary>
    public string PublicationContext { get; set; } = null!;
    public string? CommunityId { get; set; }
    public string AuthorId { get; set; } = null!;
    public UserPublicDto Author { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public List<string>? ImageUrls { get; set; }
    /// <summary>Galeria tipada (inclui vídeo). <see cref="ImageUrls"/> mantém só entradas adequadas a <c>&lt;img&gt;</c> para clientes legados.</summary>
    public List<MediaAttachmentResponseDto>? MediaAttachments { get; set; }
    public List<string>? Tags { get; set; }
    public string CreatedAt { get; set; } = null!;
    public string? UpdatedAt { get; set; }
    public string? DeletedAt { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public bool LikedByCurrentUser { get; set; }
    public PostCommunityPreviewDto? Community { get; set; }

    /// <summary>Destaque no perfil da autora; <c>null</c> se não fixado.</summary>
    public string? PinnedOnProfileAt { get; set; }

    /// <summary>Impulsionamento activo (plano premium da comunidade).</summary>
    public bool CommunityBoostActive { get; set; }

    /// <summary>Fim do impulsionamento em ISO UTC, quando <see cref="CommunityBoostActive"/> é verdadeiro.</summary>
    public string? CommunityBoostEndsAt { get; set; }
}
