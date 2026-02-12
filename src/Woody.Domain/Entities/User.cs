namespace Woody.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;

        public string? Bio { get; set; }
        public string? Pronouns { get; set; }
        public string? ProfilePic { get; set; }
        public string? BannerPic { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();

        public ICollection<Follow> Following { get; set; } = new List<Follow>();
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();

        public ICollection<UserTopic> UserTopics { get; set; } = new List<UserTopic>();
    }
}