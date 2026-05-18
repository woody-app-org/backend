using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities;

public class Story
{
    public int Id { get; set; }

    public int AuthorUserId { get; set; }
    public User Author { get; set; } = null!;

    public StoryMediaType MediaType { get; set; }

    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? StorageKey { get; set; }

    public string? Text { get; set; }
    public string? BackgroundColor { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public StoryVisibility Visibility { get; set; } = StoryVisibility.Public;

    // Reservado para fase futura (música) — não exposto no MVP.
    public string? MusicProvider { get; set; }
    public string? MusicTrackId { get; set; }
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicPreviewUrl { get; set; }

    public ICollection<StoryView> Views { get; set; } = new List<StoryView>();
}
