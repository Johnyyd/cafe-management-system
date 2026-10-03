using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IOrderRepository : IRepository<Domain.Orders.Order>
{
    Task<Domain.Orders.Order?> GetByIdWithItemsAsync(ObjectId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Orders.Order>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Orders.Order>> GetByShopIdAndStatusAsync(ObjectId shopId, OrderStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Orders.Order>> GetByDateRangeAsync(ObjectId shopId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}