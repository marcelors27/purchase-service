using Microsoft.Extensions.Configuration;
using Npgsql;

namespace PurchaseService.Api.Configuration;

public static class ConfigurationExtensions
{
    private const string SharedDatabaseUrlVariable = "DATABASE_URL";
    private const string DevelopDatabaseUrlVariable = "DATABASE_URL_DEVELOP";
    private const string ProductionDatabaseUrlVariable = "DATABASE_URL_PRODUCTION";

    public static void ConfigureDatabaseConnection(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["Database:ConnectionString"];

        if (TryNormalizeConnectionString(connectionString, out var normalized))
        {
            builder.Configuration["Database:ConnectionString"] = normalized;
            return;
        }

        var candidate = GetEnvironmentSpecificDatabaseUrl(builder.Environment.EnvironmentName, builder.Configuration);

        if (TryNormalizeConnectionString(candidate, out normalized))
        {
            builder.Configuration["Database:ConnectionString"] = normalized;
        }
    }

    private static string? GetEnvironmentSpecificDatabaseUrl(string environmentName, ConfigurationManager configuration)
    {
        var environmentSpecificVariable = environmentName switch
        {
            "Develop" => DevelopDatabaseUrlVariable,
            "Production" => ProductionDatabaseUrlVariable,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(environmentSpecificVariable))
        {
            var environmentValue = configuration[environmentSpecificVariable];
            if (!string.IsNullOrWhiteSpace(environmentValue))
            {
                return environmentValue;
            }
        }

        return configuration[SharedDatabaseUrlVariable];
    }

    private static bool TryNormalizeConnectionString(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        normalized = value.Contains("://", StringComparison.Ordinal)
            ? ConvertDatabaseUrlToConnectionString(value)
            : value;

        return true;
    }

    private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.Trim('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var credentials = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(credentials[0]);
            if (credentials.Length > 1)
            {
                builder.Password = Uri.UnescapeDataString(credentials[1]);
            }
        }

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var query = uri.Query.TrimStart('?');
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    builder[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                }
            }
        }

        return builder.ToString();
    }
}
