namespace Woody.Domain.Entities
{
    public class Topic
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public ICollection<PostTopic> PostTopics { get; set; } = new List<PostTopic>();
        public ICollection<UserTopic> UserTopics { get; set; } = new List<UserTopic>();
    }
}