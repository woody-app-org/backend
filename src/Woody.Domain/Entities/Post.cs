namespace Woody.Domain.Entities
{
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int CommunityId { get; set; }
        public Community Community { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
        public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    }
}
