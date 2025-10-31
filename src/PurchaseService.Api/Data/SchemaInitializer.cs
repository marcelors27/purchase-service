using Dapper;
using Microsoft.Extensions.Logging;

namespace PurchaseService.Api.Data;

public sealed class SchemaInitializer
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS purchases (
            id UUID PRIMARY KEY,
            description VARCHAR(50) NOT NULL,
            transaction_date DATE NOT NULL,
            amount_usd NUMERIC(18,2) NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
        );

        CREATE INDEX IF NOT EXISTS ix_purchases_transaction_date ON purchases (transaction_date);
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public SchemaInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task EnsureSchemaAsync(ILogger<SchemaInitializer> logger, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(CreateTableSql, cancellationToken: cancellationToken));
        logger.LogInformation("Database schema ensured.");
    }
}
