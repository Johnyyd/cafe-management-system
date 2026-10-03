namespace CafeManagement.Domain.Shops;

public enum ShopStatus
{
    Active = 1,
    UnderMaintenance = 2,
    Closed = 3
}

public static class ShopStatusExtensions
{
    public static bool IsOperational(this ShopStatus status) => status == ShopStatus.Active;
}