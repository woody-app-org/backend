namespace Woody.Application.Stories;

/// <summary>Agregado de stories ativos por autora (uso interno no feed de stories).</summary>
public sealed class StoryFeedAuthorSummary
{
    public int AuthorUserId { get; init; }
    public DateTime LastCreatedAt { get; init; }
    public IReadOnlyList<int> StoryIds { get; init; } = [];
}
