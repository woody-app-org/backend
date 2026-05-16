namespace Woody.Domain.Entities;

/// <summary>
/// Agregados diários por comunidade (visitas à página pública, saídas anónimas).
/// Extensível com novas colunas sem expor identidades.
/// </summary>
public class CommunityDailyRollup
{
    public int CommunityId { get; set; }
    public Community Community { get; set; } = null!;

    public DateOnly DayUtc { get; set; }

    public int PageViews { get; set; }

    /// <summary>Contagem agregada de saídas/remoções/banimentos (sem identificar utilizadoras).</summary>
    public int MemberLeaves { get; set; }
}
