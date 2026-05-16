namespace Woody.Domain.Entities
{
    public class Follow
    {
        public int FollowingUserId { get; set; }
        public User FollowingUser { get; set; } = null!;

        public int FollowedUserId { get; set; }
        public User FollowedUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}