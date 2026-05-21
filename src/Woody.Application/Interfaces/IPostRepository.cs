using Woody.Application.DTOs;
using Woody.Domain.Entities;

namespace Woody.Application.Interfaces;

public interface IPostRepository
{
    Task<List<Post>> ListNonDeletedWithNavAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts não apagados que a utilizadora pode ver: perfil; comunidade pública; ou comunidade privada com membership ativa.
    /// </summary>
    Task<List<PostFeedCandidate>> ListNonDeletedVisibleFeedCandidatesAsync(
        int? viewerUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hidrata posts por ids (com navegações para DTO). A ordem da lista devolvida segue <paramref name="ids"/>.
    /// </summary>
    Task<List<Post>> ListNonDeletedByIdsWithNavOrderedAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Publicações da utilizadora no perfil: fixados (até o limite) e página de não fixados,
    /// com as mesmas regras de visibilidade que o feed do perfil.
    /// </summary>
    Task<(List<Post> Pinned, List<Post> Items, int UnpinnedTotalCount, int AllVisibleCount)> GetProfilePostsPageAsync(
        int profileUserId,
        int? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(List<Post> Items, int Total)> ListByCommunityIdPagedAsync(int communityId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Total de posts não apagados no contexto da comunidade (para resumo analytics).</summary>
    Task<int> CountNonDeletedCommunityPostsAsync(int communityId, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdNonDeletedWithNavAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByPublicIdNonDeletedWithNavAsync(string publicId, CancellationToken cancellationToken = default);
    Task<bool> ExistsPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdTrackedWithTagsAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<Post?> GetByIdNonDeletedForCommentLookupAsync(int id, CancellationToken cancellationToken = default);
    void Add(Post post);
    Task AddPostTagsAsync(IEnumerable<PostTag> tags, CancellationToken cancellationToken = default);
    Task AddPostMediaAttachmentsAsync(IEnumerable<MediaAttachment> attachments, CancellationToken cancellationToken = default);
    void RemovePostTags(IEnumerable<PostTag> tags);
    Task<List<Post>> SearchNonDeletedWithNavAsync(string loweredQuery, int take, CancellationToken cancellationToken = default);

    /// <summary>Conta posts não apagados da autora com pin ativo no perfil.</summary>
    Task<int> CountPinnedPostsForAuthorAsync(int authorUserId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
