using Microsoft.EntityFrameworkCore;

namespace Woody.Infrastructure.Persistence.Context
{
    public class WoodyDbContext : DbContext
    {
        public WoodyDbContext(DbContextOptions<WoodyDbContext> options) : base(options)
        {
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) 
        {
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }
}