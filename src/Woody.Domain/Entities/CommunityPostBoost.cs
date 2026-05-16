namespace Woody.Domain.Entities;

/// <summary>
/// Impulsionamento de um post no contexto de uma comunidade (plano premium do espaço).
/// Activo quando não cancelado e o relógio UTC está em [StartedAtUtc, EndsAtUtc).
/// </summary>
public class CommunityPostBoost
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;

    public DateTime StartedAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    /// <summary>Cancelamento antecipado (staff); não expõe quem cancelou na API pública.</summary>
    public DateTime? CancelledAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsActiveAt(DateTime utcNow) =>
        CancelledAtUtc == null && StartedAtUtc <= utcNow && EndsAtUtc > utcNow;
}
