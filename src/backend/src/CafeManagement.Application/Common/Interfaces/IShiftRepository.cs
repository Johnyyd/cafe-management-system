using CafeManagement.Domain.Common;
using CafeManagement.Domain.Staff;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Application.Common.Interfaces;

public interface IShiftRepository : IRepository<Domain.Staff.Shift>
{
    Task<IReadOnlyList<Domain.Staff.Shift>> GetByStaffIdAsync(ObjectId staffId, DateTime? startDate = null, DateTime? endDate = null, ShiftStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Shift>> GetByShopIdAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, ShiftStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Shift>> GetUpcomingAsync(ObjectId? staffId = null, ObjectId? shopId = null, int days = 7, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.Shift>> GetNeedingCoverageAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

public interface ITimeOffRequestRepository : IRepository<Domain.Staff.TimeOffRequest>
{
    Task<IReadOnlyList<Domain.Staff.TimeOffRequest>> GetByStaffIdAsync(ObjectId staffId, DateTime? startDate = null, DateTime? endDate = null, TimeOffStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.TimeOffRequest>> GetByShopIdAsync(ObjectId shopId, DateTime? startDate = null, DateTime? endDate = null, TimeOffStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.TimeOffRequest>> GetPendingByShopAsync(ObjectId shopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.TimeOffRequest>> GetActiveAsync(ObjectId? staffId = null, ObjectId? shopId = null, CancellationToken cancellationToken = default);
}

public interface IShiftSwapRepository : IRepository<Domain.Staff.ShiftSwap>
{
    Task<IReadOnlyList<Domain.Staff.ShiftSwap>> GetByRequestingStaffIdAsync(ObjectId staffId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.ShiftSwap>> GetByRequestedStaffIdAsync(ObjectId staffId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.ShiftSwap>> GetByShopIdAsync(ObjectId shopId, ShiftSwapStatus? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.ShiftSwap>> GetPendingByStaffAsync(ObjectId staffId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain.Staff.ShiftSwap>> GetPendingForApprovalAsync(ObjectId shopId, CancellationToken cancellationToken = default);
}