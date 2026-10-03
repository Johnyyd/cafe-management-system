namespace CafeManagement.Domain.Staff;

public enum StaffRole
{
    Barista = 1,
    Cashier = 2,
    Manager = 3,
    Admin = 4
}

public enum EmploymentStatus
{
    Active = 1,
    OnLeave = 2,
    Terminated = 3
}

public static class StaffRoleExtensions
{
    public static bool IsManagement(this StaffRole role) => role is StaffRole.Manager or StaffRole.Admin;
    public static bool CanManageStaff(this StaffRole role) => role is StaffRole.Manager or StaffRole.Admin;
    public static bool CanManageInventory(this StaffRole role) => role is StaffRole.Manager or StaffRole.Admin;
    public static bool CanProcessOrders(this StaffRole role) => role != StaffRole.Admin; // Admin typically doesn't process orders
}