using PurchaseService.Api.Domain;

namespace PurchaseService.Api.Data;

public interface IPurchaseRepository
{
    Task CreateAsync(Purchase purchase, CancellationToken cancellationToken);

    Task<Purchase?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
