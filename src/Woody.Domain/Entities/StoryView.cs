namespace Woody.Domain.Entities;

public class StoryView
{
    public int Id { get; set; }

    public int StoryId { get; set; }
    public Story Story { get; set; } = null!;

    public int ViewerUserId { get; set; }
    public User Viewer { get; set; } = null!;

    public DateTime ViewedAt { get; set; }
}
