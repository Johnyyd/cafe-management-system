using CafeManagement.Domain.Common;
using CafeManagement.Domain.Menu;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IMenuItemRepository : IRepository<Domain.Menu.MenuItem>
{
    Task<Domain.Menu.MenuItem?> GetByShopIdAndNameAsync(ObjectId shopId, string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByCategoryAsync(ObjectId shopId, string category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Menu.MenuItem>> GetByStatusAsync(ObjectId shopId, MenuItemStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetCategoriesAsync(ObjectId shopId, CancellationToken cancellationToken = default);
}