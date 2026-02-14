using Woody.Application.DTOs.Topic;
using Woody.Application.DTOs.User;

namespace Woody.Application.DTOs.Post
{
    public class GetPostResponseDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<GetTopicResponseDTO> Topics { get; set; } = new List<GetTopicResponseDTO>();
        public UserPostResponseDTO User { get; set; } = null!;
    }
}