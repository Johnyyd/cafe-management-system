namespace CafeManagement.Domain.Orders;

public enum OrderStatus
{
    Placed = 1,
    Preparing = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public static class OrderStatusExtensions
{
    public static bool CanBeModified(this OrderStatus status) => status is OrderStatus.Placed;
    public static bool CanBeCancelled(this OrderStatus status) => status is OrderStatus.Placed or OrderStatus.Preparing;
    public static bool IsTerminal(this OrderStatus status) => status is OrderStatus.Completed or OrderStatus.Cancelled;
}

public static class PaymentStatusExtensions
{
    public static bool CanBeRefunded(this PaymentStatus status) => status == PaymentStatus.Paid;
    public static bool IsSuccessful(this PaymentStatus status) => status == PaymentStatus.Paid;
}