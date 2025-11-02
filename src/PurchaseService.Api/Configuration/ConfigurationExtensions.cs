using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace PurchaseService.Api.Configuration;

public static class ConfigurationExtensions
{
    private const string SharedDatabaseUrlVariable = "DATABASE_URL";
    private const string DevelopDatabaseUrlVariable = "DATABASE_URL_DEVELOP";
    private const string ProductionDatabaseUrlVariable = "DATABASE_URL_PRODUCTION";

    public static IServiceCollection ConfigureDatabaseOptions(this IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        services.Configure<DatabaseOptions>(options =>
        {
            foreach (var candidate in GetConnectionStringCandidates(configuration, environmentName))
            {
                if (TryNormalizeConnectionString(candidate, out var normalized))
                {
                    options.ConnectionString = normalized;
                    return;
                }
            }
        });

        return services;
    }

    private static IEnumerable<string?> GetConnectionStringCandidates(IConfiguration configuration, string environmentName)
    {
        yield return configuration["Database:ConnectionString"];
        yield return configuration["ConnectionStrings:Default"];

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            yield return configuration[$"Database:ConnectionString:{environmentName}"];
            yield return configuration[$"ConnectionStrings:{environmentName}"];
        }

        yield return GetEnvironmentSpecificDatabaseUrl(environmentName, configuration);
    }

    private static string? GetEnvironmentSpecificDatabaseUrl(string environmentName, IConfiguration configuration)
    {
        var variables = new List<string>();

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            var trimmed = environmentName.Trim();
            variables.Add($"DATABASE_URL_{trimmed.ToUpperInvariant()}");

            if (trimmed.Equals("Develop", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                variables.Add(DevelopDatabaseUrlVariable);
                variables.Add("DATABASE_URL_DEVELOPMENT");
            }
            else if (trimmed.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                variables.Add(ProductionDatabaseUrlVariable);
            }
        }

        variables.Add(SharedDatabaseUrlVariable);

        foreach (var variable in variables)
        {
            var value = configuration[variable];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryNormalizeConnectionString(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();

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
            SslMode = SslMode.Require
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
