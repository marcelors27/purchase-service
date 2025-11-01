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
        SELECT
            id AS Id,
            description AS Description,
            transaction_date AS TransactionDate,
            amount_usd AS AmountUsd,
            created_at AS CreatedAt
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
        var parameters = new
        {
            purchase.Id,
            purchase.Description,
            TransactionDate = purchase.TransactionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            purchase.AmountUsd,
            CreatedAt = purchase.CreatedAt.UtcDateTime
        };

        await connection.ExecuteAsync(new CommandDefinition(InsertSql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PurchaseRow>(
            new CommandDefinition(GetByIdSql, new { Id = id }, cancellationToken: cancellationToken));

        return row is null ? null : MapToDomain(row);
    }

    private static Purchase MapToDomain(PurchaseRow row)
    {
        var transactionDate = DateOnly.FromDateTime(row.TransactionDate);
        var createdAtUtc = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc);
        var createdAt = new DateTimeOffset(createdAtUtc, TimeSpan.Zero);

        return new Purchase(row.Id, row.Description, transactionDate, row.AmountUsd, createdAt);
    }

    private sealed record PurchaseRow(
        Guid Id,
        string Description,
        DateTime TransactionDate,
        decimal AmountUsd,
        DateTime CreatedAt);
}
