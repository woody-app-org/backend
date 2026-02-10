using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence
{
    public class WoodyDbContextFactory 
        : IDesignTimeDbContextFactory<WoodyDbContext>
    {
        public WoodyDbContext CreateDbContext(string[] args)
        {
            var connectionString =
                $"Host={Environment.GetEnvironmentVariable("DB_HOST")};" +
                $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                $"Database=woody_db;" +
                $"Username={Environment.GetEnvironmentVariable("DB_USERNAME")};" +
                $"Password={Environment.GetEnvironmentVariable("DB_PASS")};" +
                $"SearchPath=public";

            var optionsBuilder = new DbContextOptionsBuilder<WoodyDbContext>();
            optionsBuilder
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention();

            return new WoodyDbContext(optionsBuilder.Options);
        }
    }
}
