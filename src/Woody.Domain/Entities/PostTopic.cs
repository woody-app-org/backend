namespace Woody.Domain.Entities
{
    public class PostTopic
    {
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public int TopicId { get; set; }
        public Topic Topic { get; set; } = null!;
    }
}