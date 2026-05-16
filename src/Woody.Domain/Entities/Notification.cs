using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

/// <summary>
/// Notificação in-app. A relação principal é (TargetKind + TargetId); <see cref="MetadataJson"/> complementa
/// navegação (ex.: slug, ids secundários) sem substituir FKs importantes.
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public int RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;

    public int? ActorUserId { get; set; }
    public User? ActorUser { get; set; }

    public NotificationType Type { get; set; }

    public NotificationTargetKind TargetKind { get; set; }

    /// <summary>Identificador da entidade indicada em <see cref="TargetKind"/>; null quando não aplicável.</summary>
    public int? TargetId { get; set; }

    public string? Title { get; set; }

    public string? Message { get; set; }

    /// <summary>JSON camelCase com dados auxiliares (navegação, ids adicionais).</summary>
    public string MetadataJson { get; set; } = "{}";

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
