using Woody.Application.DTOs.Post;
using Woody.Domain.Entities;
using Woody.Domain.Interfaces;

namespace Woody.Application.UseCases.Posts.CreatePost
{
    public class CreatePostHandler
    {
        private readonly IPostRepository _postRepository;
        
        public CreatePostHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<bool> Handle(CreatePostRequestDTO request)
        {
            var post = new Post
            {
                Content = request.Content,
                UserId = request.UserId,
                PostTopics = request.TopicIds.Select(tid => new PostTopic { TopicId = tid }).ToList(),
                CreatedAt = DateTime.Now.ToUniversalTime()
            };

            await _postRepository.AddAsync(post);

            return true;
        }
    }
}