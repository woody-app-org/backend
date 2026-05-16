namespace Woody.Application.DTOs.Api;

/// <summary>
/// Posts visíveis no perfil: fixados em destaque (até o limite) e lista paginada sem fixados.
/// </summary>
public class ProfilePostsPageResponseDto
{
    public List<PostResponseDto> Pinned { get; set; } = new();

    public List<PostResponseDto> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    /// <summary>Total de publicações visíveis no perfil (fixadas + não fixadas).</summary>
    public int TotalCount { get; set; }

    /// <summary>Total só das não fixadas (base da paginação de <see cref="Items"/>).</summary>
    public int UnpinnedTotalCount { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }
}
