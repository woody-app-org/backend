using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Woody.Infrastructure.Persistence.Context;

namespace Woody.Infrastructure.Persistence.Configuration;

public class WoodyDbContextFactory : IDesignTimeDbContextFactory<WoodyDbContext>
{
    public WoodyDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildDesignTimeConfiguration();
        var connectionString = DatabaseConnectionResolver.Resolve(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<WoodyDbContext>();
        optionsBuilder
            .UseNpgsql(
                connectionString,
                x => x
                    .MigrationsHistoryTable(HistoryRepository.DefaultTableName, "public")
                    .MigrationsAssembly("Woody.Infrastructure"))
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging();

        return new WoodyDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildDesignTimeConfiguration()
    {
        var builder = new ConfigurationBuilder();

        var apiDirectory = FindWoodyApiDirectory();
        if (apiDirectory != null)
        {
            builder.SetBasePath(apiDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false);
        }

        builder.AddEnvironmentVariables();
        return builder.Build();
    }

    private static string? FindWoodyApiDirectory()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Woody.Api");
            if (File.Exists(Path.Combine(candidate, "Woody.Api.csproj")))
                return candidate;

            var siblingApi = Path.Combine(dir.FullName, "Woody.Api");
            if (File.Exists(Path.Combine(siblingApi, "Woody.Api.csproj")))
                return siblingApi;

            dir = dir.Parent;
        }

        return null;
    }
}
