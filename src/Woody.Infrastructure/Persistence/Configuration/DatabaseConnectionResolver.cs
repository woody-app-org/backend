using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Woody.Infrastructure.Persistence.Configuration;

/// <summary>
/// Resolve a connection string Npgsql a partir de configuração ou DATABASE_URL (formato Railway/Heroku).
/// </summary>
public static class DatabaseConnectionResolver
{
    private const string PublicSchema = "public";

    public static string Resolve(IConfiguration configuration)
    {
        var fromConfig = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return EnsureSearchPath(fromConfig.Trim());

        var databaseUrl =
            configuration["DATABASE_URL"]
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return EnsureSearchPath(ConvertDatabaseUrl(databaseUrl.Trim()));

        throw new InvalidOperationException(
            "Defina a conexão com o PostgreSQL: variável ConnectionStrings__DefaultConnection " +
            "ou DATABASE_URL (ex.: postgres://usuario:senha@host:porta/nome_do_banco).");
    }

    private static string EnsureSearchPath(string connectionString)
    {
        if (connectionString.Contains("Search Path=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("SearchPath=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        return connectionString.TrimEnd(';') + $";Search Path={PublicSchema}";
    }

    /// <summary>
    /// Converte postgres:// ou postgresql:// em connection string Npgsql.
    /// </summary>
    public static string ConvertDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            throw new InvalidOperationException(
                "DATABASE_URL deve ser uma URI absoluta com esquema postgres:// ou postgresql://.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
            throw new InvalidOperationException("DATABASE_URL sem host.");

        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrEmpty(database))
            database = "postgres";

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = user,
            Password = password,
            Pooling = true
        };

        ApplyQueryParameters(uri.Query, builder);

        return builder.ConnectionString;
    }

    private static void ApplyQueryParameters(string query, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrEmpty(query))
            return;

        var trimmed = query.TrimStart('?');
        foreach (var segment in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = value.ToLowerInvariant() switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "require" => SslMode.Require,
                    "verify-ca" => SslMode.VerifyCA,
                    "verify-full" => SslMode.VerifyFull,
                    _ => SslMode.Prefer
                };
            }
        }
    }
}
