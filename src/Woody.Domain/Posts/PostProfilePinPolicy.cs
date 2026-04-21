using Woody.Domain.Entities;

namespace Woody.Domain.Posts;

/// <summary>
/// Regras puras para destacar publicações da autora no próprio perfil (limite e titularidade).
/// Persistência e contagens ficam na camada de aplicação/infra.
/// </summary>
public static class PostProfilePinPolicy
{
    public const int MaxPinnedPostsOnProfile = 3;

    /// <summary>
    /// A utilizadora só pode gerir pins no perfil para as próprias publicações não apagadas.
    /// Inclui posts de perfil e de comunidade, desde que a autora seja ela (aparecem no seu perfil).
    /// </summary>
    public static bool CanPinPostOnProfile(int actingUserId, Post post) =>
        post.DeletedAt == null && post.UserId == actingUserId;

    public static bool CanUnpinPostOnProfile(int actingUserId, Post post) =>
        CanPinPostOnProfile(actingUserId, post);
}
