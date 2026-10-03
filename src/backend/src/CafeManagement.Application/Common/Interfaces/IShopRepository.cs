using CafeManagement.Domain.Common;
using CafeManagement.Domain.Shops;

namespace CafeManagement.Application.Common.Interfaces;

public interface IShopRepository : IRepository<Shop>
{
    Task<Shop?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shop>> GetByStatusAsync(ShopStatus status, CancellationToken cancellationToken = default);
}