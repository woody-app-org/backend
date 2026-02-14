using Woody.Domain.Entities;

namespace Woody.Domain.Interfaces
{
    public interface IPostRepository
    {
        Task AddAsync(Post post);
        Task<Post?> GetByIdAsync(int id);
        Task<IEnumerable<Post>> GetByTopicIdAsync(int topicId, int page, int pageSize);
        Task<IEnumerable<Post>> GetByUserIdAsync(int userId, int page, int pageSize);
    }
}