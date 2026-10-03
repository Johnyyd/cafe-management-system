using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IStaffRepository : IRepository<Domain.Staff.Staff>
{
    Task<Domain.Staff.Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Staff>> GetByShopIdAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Staff>> GetByRoleAsync(StaffRole role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Staff>> GetByEmploymentStatusAsync(EmploymentStatus status, CancellationToken cancellationToken = default);
}