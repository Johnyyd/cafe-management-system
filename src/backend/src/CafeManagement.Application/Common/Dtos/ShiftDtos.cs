using CafeManagement.Domain.Staff;
using MongoDB.Bson;

namespace CafeManagement.Application.Common.Dtos;

public record ShiftDto(
    ObjectId Id,
    ObjectId StaffId,
    ObjectId ShopId,
    DateTime StartTime,
    DateTime EndTime,
    ShiftStatus Status,
    string? Notes,
    ObjectId? CoveredByStaffId,
    DateTime? CoveredAt,
    ObjectId? CoveredBy,
    DateTime? SwappedAt,
    ObjectId? SwappedWithStaffId,
    ObjectId? SwappedWithShiftId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record ShiftSummaryDto(
    ObjectId Id,
    ObjectId StaffId,
    string StaffName,
    ObjectId ShopId,
    DateTime StartTime,
    DateTime EndTime,
    ShiftStatus Status
);

public record TimeOffRequestDto(
    ObjectId Id,
    ObjectId StaffId,
    ObjectId ShopId,
    TimeOffType Type,
    DateTime StartDate,
    DateTime EndDate,
    TimeOffStatus Status,
    string? Reason,
    ObjectId? ApprovedBy,
    DateTime? ApprovedAt,
    string? ApprovalNotes,
    ObjectId? RejectedBy,
    DateTime? RejectedAt,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record TimeOffRequestSummaryDto(
    ObjectId Id,
    ObjectId StaffId,
    string StaffName,
    TimeOffType Type,
    DateTime StartDate,
    DateTime EndDate,
    TimeOffStatus Status
);

public record ShiftSwapDto(
    ObjectId Id,
    ObjectId RequestingStaffId,
    string RequestingStaffName,
    ObjectId RequestedStaffId,
    string RequestedStaffName,
    ObjectId RequestingShiftId,
    ObjectId RequestedShiftId,
    ObjectId ShopId,
    ShiftSwapStatus Status,
    string? Reason,
    ObjectId? ApprovedBy,
    DateTime? ApprovedAt,
    string? ApprovalNotes,
    ObjectId? RejectedBy,
    DateTime? RejectedAt,
    string? RejectionReason,
    ObjectId? CancelledBy,
    DateTime? CancelledAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public record ShiftSwapSummaryDto(
    ObjectId Id,
    ObjectId RequestingStaffId,
    string RequestingStaffName,
    ObjectId RequestedStaffId,
    string RequestedStaffName,
    ObjectId RequestingShiftId,
    ObjectId RequestedShiftId,
    ShiftSwapStatus Status
);