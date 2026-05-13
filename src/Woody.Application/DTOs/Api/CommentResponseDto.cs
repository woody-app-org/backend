namespace Woody.Application.DTOs.Api;

public class CommentResponseDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? ParentCommentId { get; set; }
    public string AuthorId { get; set; } = null!;
    public UserPublicDto Author { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string CreatedAt { get; set; } = null!;
    public string? DeletedAt { get; set; }
    public string? HiddenByPostAuthorAt { get; set; }
    public string? ContentModerationMask { get; set; }

    /// <summary>Destaque escolhido pela autora do post; <c>null</c> se não fixo.</summary>
    public string? PinnedOnPostAt { get; set; }

    public int LikesCount { get; set; }

    /// <summary>Autenticado: se a utilizadora atual deu gosto neste comentário; anónimo: sempre falso.</summary>
    public bool LikedByCurrentUser { get; set; }

    /// <summary>GIF anexado; omitido quando o comentário não tem GIF ou o conteúdo está oculto ao viewer.</summary>
    public CommentGifResponseDto? Gif { get; set; }
}
