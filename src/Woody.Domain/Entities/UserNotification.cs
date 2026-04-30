namespace Woody.Domain.Entities;

/// <summary>
/// Notificação in-app persistida por utilizadora destinatária.
/// O tipo é texto estável (ex.: post_like) para extensibilidade sem migrações por valor enum.
/// </summary>
public class UserNotification
{
    public int Id { get; set; }

    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    /// <summary>Utilizadora que originou o evento; null para eventos só de sistema (raro).</summary>
    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    /// <summary>Ex.: post_like, post_comment, comment_reply, new_follower, profile_signal, message_request, community_request, community_request_approved.</summary>
    public string Type { get; set; } = null!;

    /// <summary>JSON com contexto de navegação (postId, commentId, communitySlug, etc.).</summary>
    public string PayloadJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }
}
