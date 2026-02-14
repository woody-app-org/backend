namespace Woody.Application.DTOs.Post
{
    public class CreatePostRequestDTO
    {
        public string Content { get; set; } = null!;
        public int UserId { get; set; }
        public List<int> TopicIds { get; set; } = new List<int>();
    }
}