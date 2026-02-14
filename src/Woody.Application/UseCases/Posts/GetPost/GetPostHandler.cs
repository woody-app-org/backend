using Woody.Application.DTOs.Post;
using Woody.Application.DTOs.Topic;
using Woody.Application.DTOs.User;
using Woody.Domain.Interfaces;

namespace Woody.Application.UseCases.Posts.GetPost
{
    public class GetPostHandler
    {
        private readonly IPostRepository _postRepository;
        
        public GetPostHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<GetPostResponseDTO?> HandleGetById(int postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) return null;

            return new GetPostResponseDTO
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                Topics = post.PostTopics.Select(pt => new GetTopicResponseDTO { Id = pt.Topic.Id, Name = pt.Topic.Name }).ToList(),
                User = new UserPostResponseDTO
                {
                    Id = post.User.Id,
                    Username = post.User.Username,
                    ProfilePic = post.User.ProfilePic ?? "default.png"
                }
            };
        }

        public async Task<IEnumerable<GetPostResponseDTO>> HandleGetByTopicId(int topicId, int page, int pageSize)
        {
            var posts = await _postRepository.GetByTopicIdAsync(topicId, page, pageSize);

            if (!posts.Any()) return Enumerable.Empty<GetPostResponseDTO>();

            return posts.Select(post => new GetPostResponseDTO
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                Topics = post.PostTopics.Select(pt => new GetTopicResponseDTO { Id = pt.Topic.Id, Name = pt.Topic.Name }).ToList(),
                User = new UserPostResponseDTO
                {
                    Id = post.User.Id,
                    Username = post.User.Username,
                    ProfilePic = post.User.ProfilePic ?? "default.png"
                }
            }).ToList();
        }

        public async Task<IEnumerable<GetPostResponseDTO>> HandleGetByUserId(int userId, int page, int pageSize)
        {
            var posts = await _postRepository.GetByUserIdAsync(userId, page, pageSize);

            if (!posts.Any()) return Enumerable.Empty<GetPostResponseDTO>();

            return posts.Select(post => new GetPostResponseDTO
            {
                Id = post.Id,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                Topics = post.PostTopics.Select(pt => new GetTopicResponseDTO { Id = pt.Topic.Id, Name = pt.Topic.Name }).ToList(),
                User = new UserPostResponseDTO
                {
                    Id = post.User.Id,
                    Username = post.User.Username,
                    ProfilePic = post.User.ProfilePic ?? "default.png"
                }
            }).ToList();
        }
    }
}