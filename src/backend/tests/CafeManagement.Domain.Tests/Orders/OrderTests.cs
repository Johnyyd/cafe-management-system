using CafeManagement.Domain.Common;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Orders.Events;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.Domain.Tests.Orders;

public class OrderTests
{
    private readonly ObjectId _shopId = ObjectId.GenerateNewId();
    private readonly ObjectId _staffId = ObjectId.GenerateNewId();
    private readonly ObjectId _menuItemId = ObjectId.GenerateNewId();
    private readonly CustomerInfo _customerInfo = new("Test Customer", "0123456789", CustomerType.DineIn);

    private List<OrderItem> CreateValidItems()
    {
        var unitPrice = new Money(50000, "VND");
        return new List<OrderItem>
        {
            new OrderItem(_menuItemId, "Test Latte", "A delicious test latte", unitPrice, 2)
        };
    }

    [Fact]
    public void Create_ValidInput_ReturnsSuccessWithOrderNumber()
    {
        // Act
        var result = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems());

        // Assert
        Assert.True(result.IsSuccess);
        var order = result.Value;
        Assert.NotNull(order.OrderNumber);
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Matches(@"^ORD-\d{6}-[A-F0-9]{6}$", order.OrderNumber);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
        Assert.Equal(100000, order.TotalAmount.Amount);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Create_EmptyItems_ReturnsFailure()
    {
        // Act
        var result = Order.Create(_shopId, _staffId, _customerInfo, new List<OrderItem>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Items"));
    }

    [Fact]
    public void Create_NullCustomerInfo_ReturnsFailure()
    {
        // Act
        var result = Order.Create(_shopId, _staffId, null!, CreateValidItems());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("CustomerInfo"));
    }

    [Fact]
    public void GenerateOrderNumber_ProducesCorrectFormat()
    {
        // Arrange
        var id = ObjectId.GenerateNewId();

        // Act
        var orderNumber = Order.GenerateOrderNumber(id);

        // Assert
        Assert.StartsWith("ORD-", orderNumber);
        Assert.Matches(@"^ORD-\d{6}-[A-F0-9]{6}$", orderNumber);
    }

    [Fact]
    public void GenerateOrderNumber_UsesTimestampFromObjectId()
    {
        // Arrange - Create ObjectId with known timestamp (2024-01-15)
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var id = ObjectId.GenerateNewId(timestamp);

        // Act
        var orderNumber = Order.GenerateOrderNumber(id);

        // Assert
        Assert.Contains("240115", orderNumber); // yyMMdd format
    }

    [Fact]
    public void ChangeStatus_ValidTransition_ReturnsSuccess()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act - Placed -> Preparing
        var result = order.ChangeStatus(OrderStatus.Preparing);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act - Placed -> Ready (skipping Preparing)
        var result = order.ChangeStatus(OrderStatus.Ready);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot transition"));
        Assert.Equal(OrderStatus.Placed, order.Status); // Status unchanged
    }

    [Fact]
    public void ChangeStatus_Completed_RequiresPayment()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);

        // Act - Try to complete without payment
        var result = order.ChangeStatus(OrderStatus.Completed);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Payment is required before completing"));
    }

    [Fact]
    public void ChangeStatus_Completed_AfterPayment_Succeeds()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);
        order.ProcessCashPayment(new Money(100000, "VND"));

        // Act
        var result = order.ChangeStatus(OrderStatus.Completed);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedTime);
    }

    [Fact]
    public void ChangeStatus_Cancelled_FromPlaced_Succeeds()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.ChangeStatus(OrderStatus.Cancelled);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void ChangeStatus_Cancelled_FromPaid_RefundsPayment()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);
        order.ProcessCashPayment(new Money(100000, "VND"));
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);

        // Act
        var result = order.ChangeStatus(OrderStatus.Cancelled);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Refunded, order.PaymentStatus);
    }

    [Fact]
    public void ChangeStatus_TerminalStatus_CannotChange()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);
        order.ProcessCashPayment(new Money(100000, "VND"));
        order.ChangeStatus(OrderStatus.Completed);

        // Act - Try to change from Completed
        var result = order.ChangeStatus(OrderStatus.Preparing);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("terminal status"));
    }

    [Fact]
    public void ProcessCashPayment_ValidAmount_Succeeds()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.ProcessCashPayment(new Money(100000, "VND"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
    }

    [Fact]
    public void ProcessCashPayment_InsufficientAmount_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.ProcessCashPayment(new Money(50000, "VND")); // Only half

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("less than the order total"));
        Assert.Equal(PaymentStatus.Pending, order.PaymentStatus);
    }

    [Fact]
    public void ProcessCashPayment_WrongCurrency_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.ProcessCashPayment(new Money(100000, "USD"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Currency"));
    }

    [Fact]
    public void ProcessCashPayment_EmptyOrder_ReturnsFailure()
    {
        // Arrange - Order with no items (total = 0)
        var customerInfo = new CustomerInfo("Test", "0123456789", CustomerType.DineIn);
        var order = new Order(ObjectId.GenerateNewId(), _shopId, _staffId, customerInfo, new List<OrderItem>(), DateTime.UtcNow);
        // Note: Can't create via Order.Create because it validates items

        // Act
        var result = order.ProcessCashPayment(new Money(100, "VND"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("empty order"));
    }

    [Fact]
    public void ProcessCashPayment_AlreadyPaid_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ProcessCashPayment(new Money(100000, "VND"));

        // Act
        var result = order.ProcessCashPayment(new Money(100000, "VND"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already been paid"));
    }

    [Fact]
    public void PayAndComplete_ValidFlow_ReturnsSuccess()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);

        // Act
        var result = order.PayAndComplete(new Money(100000, "VND"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
        Assert.NotNull(order.CompletedTime);
    }

    [Fact]
    public void PayAndComplete_NotReadyStatus_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        // Status is Placed, not Ready

        // Act
        var result = order.PayAndComplete(new Money(100000, "VND"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot transition"));
    }

    [Fact]
    public void PayAndComplete_InsufficientAmount_ReturnsFailure()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        order.ChangeStatus(OrderStatus.Preparing);
        order.ChangeStatus(OrderStatus.Ready);

        // Act
        var result = order.PayAndComplete(new Money(50000, "VND"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("less than the order total"));
    }

    [Fact]
    public void Cancel_DelegatesToChangeStatus()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void AddItem_ValidItem_UpdatesTotal()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        var newItem = new OrderItem(ObjectId.GenerateNewId(), "Cake", "Chocolate cake", new Money(30000, "VND"), 1);

        // Act
        var result = order.AddItem(newItem);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(130000, order.TotalAmount.Amount);
    }

    [Fact]
    public void RemoveItem_ExistingItem_UpdatesTotal()
    {
        // Arrange
        var items = CreateValidItems();
        items.Add(new OrderItem(ObjectId.GenerateNewId(), "Cake", "Chocolate cake", new Money(30000, "VND"), 1));
        var order = Order.Create(_shopId, _staffId, _customerInfo, items).Value;

        // Act
        var result = order.RemoveItem(_menuItemId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(order.Items);
        Assert.Equal(30000, order.TotalAmount.Amount);
    }

    [Fact]
    public void UpdateItemQuantity_ValidQuantity_UpdatesTotal()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;

        // Act
        var result = order.UpdateItemQuantity(_menuItemId, 3);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, order.Items[0].Quantity);
        Assert.Equal(150000, order.TotalAmount.Amount);
    }

    [Fact]
    public void UpdateCustomerInfo_ValidInfo_ReturnsSuccess()
    {
        // Arrange
        var order = Order.Create(_shopId, _staffId, _customerInfo, CreateValidItems()).Value;
        var newInfo = new CustomerInfo("New Customer", "0987654321", CustomerType.Takeaway);

        // Act
        var result = order.UpdateCustomerInfo(newInfo);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Customer", order.CustomerInfo.Name);
        Assert.Equal("0987654321", order.CustomerInfo.Contact);
        Assert.Equal(CustomerType.Takeaway, order.CustomerInfo.Type);
    }
}