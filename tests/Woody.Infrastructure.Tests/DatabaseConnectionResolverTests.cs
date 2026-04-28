using Microsoft.Extensions.Configuration;
using Npgsql;
using Woody.Infrastructure.Persistence.Configuration;

namespace Woody.Infrastructure.Tests;

public class DatabaseConnectionResolverTests
{
    [Fact]
    public void ConvertDatabaseUrl_ParsesPostgresUrlAndPreservesSslMode()
    {
        var connectionString = DatabaseConnectionResolver.ConvertDatabaseUrl(
            "postgresql://woody_user:woody%40123@db.example.com:5432/woody_db?sslmode=require");

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("db.example.com", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("woody_db", parsed.Database);
        Assert.Equal("woody_user", parsed.Username);
        Assert.Equal("woody@123", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Resolve_AppendsPublicSearchPath()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=woody_db;Username=woody_user;Password=local"
            })
            .Build();

        var connectionString = DatabaseConnectionResolver.Resolve(configuration);

        Assert.Contains("Search Path=public", connectionString, StringComparison.OrdinalIgnoreCase);
    }
}
