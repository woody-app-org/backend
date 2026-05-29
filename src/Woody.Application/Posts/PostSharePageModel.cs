namespace Woody.Application.Posts;

public sealed class PostSharePageModel
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string ImageUrl { get; init; }
    public required string SharePageUrl { get; init; }
    public required string FrontendPostUrl { get; init; }
    public required bool IsUnavailable { get; init; }
    public string? PublicId { get; init; }
}
