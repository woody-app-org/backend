namespace Woody.Application.DTOs.Api;

public sealed class CommunityPostBoostActivateRequestDto
{
    /// <summary>Duração em dias (1–14). Omisso = 7.</summary>
    public int? DurationDays { get; set; }
}

public sealed class CommunityPostBoostResponseDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string CommunityId { get; set; } = null!;
    public string StartedAtUtc { get; set; } = null!;
    public string EndsAtUtc { get; set; } = null!;
    public bool Active { get; set; }
}

public sealed class CommunityPostBoostListItemDto
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    /// <summary>Pré-visualização do texto do post (sem título).</summary>
    public string? PostContentPreview { get; set; }
    public string StartedAtUtc { get; set; } = null!;
    public string EndsAtUtc { get; set; } = null!;
    public bool Active { get; set; }
}
