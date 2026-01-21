using Microsoft.EntityFrameworkCore;
using Woody.Domain.Entities;

namespace Woody.Infrastructure.Persistence.Context
{
    public class WoodyDbContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public WoodyDbContext(DbContextOptions<WoodyDbContext> options) : base(options)
        {
            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}