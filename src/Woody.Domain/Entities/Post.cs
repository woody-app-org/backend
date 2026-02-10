namespace Woody.Domain.Entities
{    
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}