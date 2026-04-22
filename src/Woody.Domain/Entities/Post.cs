using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities
{
    public class Post
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        /// <summary>Comunidade quando <see cref="PublicationContext"/> é <see cref="PostPublicationContext.Community"/>; null para post de perfil.</summary>
        public int? CommunityId { get; set; }
        public Community? Community { get; set; }

        public PostPublicationContext PublicationContext { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        /// <summary>Quando não nulo, o post está destacado no perfil da autora (limite definido em <c>PostProfilePinPolicy</c>).</summary>
        public DateTime? PinnedOnProfileAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
        public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    }
}
