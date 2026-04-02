using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence.Configuration;

public static class WoodyDbConfiguration
{
    private static readonly string Schema = "public";

    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = DatabaseConnectionResolver.Resolve(configuration);

        services.AddDbContext<WoodyDbContext>(
            (_, options) =>
            {
                options
                    .UseSnakeCaseNamingConvention()
                    .UseNpgsql(
                        connectionString,
                        x => x
                            .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema)
                            .MigrationsAssembly("Woody.Infrastructure"));

                if (environment.IsDevelopment())
                    options.EnableSensitiveDataLogging();
            },
            ServiceLifetime.Transient);
    }
}
