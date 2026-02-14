using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;
using Woody.Domain.Interfaces;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly WoodyDbContext _context;

        public PostRepository(WoodyDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Post post)
        {
            await _context.Posts.AddAsync(post);
            await _context.SaveChangesAsync();
        }

        public async Task<Post?> GetByIdAsync(int id) => await _context.Posts
        .Include(p => p.PostTopics)
            .ThenInclude(pt => pt.Topic)
        .Include(p => p.User)
        .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<IEnumerable<Post>> GetByTopicIdAsync(int topicId, int page, int pageSize) => await _context.Posts
        .Include(p => p.PostTopics)
            .ThenInclude(pt => pt.Topic)
        .Include(p => p.User)
        .Where(p => p.PostTopics.Any(pt => pt.TopicId == topicId))
        .OrderBy(p => p.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        public async Task<IEnumerable<Post>> GetByUserIdAsync(int userId, int page, int pageSize) => await _context.Posts
        .Include(p => p.PostTopics)
            .ThenInclude(pt => pt.Topic)
        .Include(p => p.User)
        .Where(p => p.UserId == userId)
        .OrderBy(p => p.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    }
}