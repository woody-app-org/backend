using Woody.Domain.Entities.Enum;

namespace Woody.Domain.Entities
{
    public class Like
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public LikeTargetType TargetType { get; set; }
        public int TargetId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}