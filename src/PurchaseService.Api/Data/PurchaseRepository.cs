using Dapper;
using PurchaseService.Api.Domain;

namespace PurchaseService.Api.Data;

public sealed class PurchaseRepository : IPurchaseRepository
{
    private const string InsertSql = """
        INSERT INTO purchases (id, description, transaction_date, amount_usd, created_at)
        VALUES (@Id, @Description, @TransactionDate, @AmountUsd, @CreatedAt);
        """;

    private const string GetByIdSql = """
        SELECT id, description, transaction_date, amount_usd, created_at AS CreatedAt
        FROM purchases
        WHERE id = @Id;
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public PurchaseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(Purchase purchase, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(InsertSql, purchase, cancellationToken: cancellationToken));
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Purchase>(
            new CommandDefinition(GetByIdSql, new { Id = id }, cancellationToken: cancellationToken));
    }
}
