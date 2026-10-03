namespace CafeManagement.Domain.Menu;

public enum MenuItemStatus
{
    Available = 1,
    Unavailable = 2,
    Seasonal = 3
}

public static class MenuItemStatusExtensions
{
    public static bool CanBeOrdered(this MenuItemStatus status) => status == MenuItemStatus.Available;
}