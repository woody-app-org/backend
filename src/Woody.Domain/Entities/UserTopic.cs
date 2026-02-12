namespace Woody.Domain.Entities
{
    public class UserTopic
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int TopicId { get; set; }
        public Topic Topic { get; set; } = null!;
    }
}