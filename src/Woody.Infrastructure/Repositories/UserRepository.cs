using Microsoft.EntityFrameworkCore;
using Woody.Application.Interfaces;
using Woody.Domain.Entities;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly WoodyDbContext _context;

        public UserRepository(WoodyDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email) => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}