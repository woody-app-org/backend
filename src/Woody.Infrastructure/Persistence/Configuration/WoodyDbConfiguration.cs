using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence.Configuration
{
    public class WoodyDbConfiguration
    {
        private readonly static string schema = "woody";
        internal static string ConnectionString;

        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = DbConnectionString(configuration);
            ConnectionString = connectionString;

            services.AddDbContext<WoodyDbContext>(
                options => options
                    .UseSnakeCaseNamingConvention()
                    .UseNpgsql(connectionString, x => x.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema)
                    .MigrationsAssembly("Woody.Infrastructure.Persistence")), ServiceLifetime.Transient);
        }

        public static string DbConnectionString(IConfiguration configuration)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var username = Environment.GetEnvironmentVariable("DB_USERNAME");
            var password = Environment.GetEnvironmentVariable("DB_PASS");
            var host = Environment.GetEnvironmentVariable("DB_HOST");
            var port = Environment.GetEnvironmentVariable("DB_PORT");

            Console.WriteLine($"Environment: {env} Username: {username} Host: {host} Port: {port}");

            var connectionString = $"Host={host};Port={port};Pooling=true;Database=woody_db;Username={username};Password={password};SearchPath={schema}";
            return connectionString;
        }
    }
}