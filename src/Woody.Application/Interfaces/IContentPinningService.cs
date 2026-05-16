namespace Woody.Application.Interfaces;

public enum ContentPinningOutcome
{
    Success,
    PostNotFound,
    CommentNotFound,
    Forbidden,
    ProfilePinLimitReached,
    CommentNotEligible
}

/// <summary>
/// Operações de fixação no perfil e no post; permissões delegadas às políticas de domínio.
/// </summary>
public interface IContentPinningService
{
    Task<ContentPinningOutcome> PinPostOnProfileAsync(int actingUserId, int postId, CancellationToken cancellationToken = default);

    Task<ContentPinningOutcome> UnpinPostOnProfileAsync(int actingUserId, int postId, CancellationToken cancellationToken = default);

    Task<ContentPinningOutcome> PinCommentOnPostAsync(int actingUserId, int postId, int commentId, CancellationToken cancellationToken = default);

    Task<ContentPinningOutcome> UnpinCommentOnPostAsync(int actingUserId, int postId, int commentId, CancellationToken cancellationToken = default);
}
