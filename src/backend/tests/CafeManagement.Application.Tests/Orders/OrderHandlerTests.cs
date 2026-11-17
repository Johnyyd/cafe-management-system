using CafeManagement.Application.Common.Interfaces;
using CafeManagement.Application.Inventory.Handlers;
using CafeManagement.Application.Inventory.Queries;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Orders.Handlers;
using CafeManagement.Application.Orders.Queries;
using CafeManagement.Domain.Common;
using CafeManagement.Domain.Inventory;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Shared;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.Application.Tests.Orders;

public class PayAndCompleteHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<PayAndCompleteHandler>> _loggerMock;
    private readonly PayAndCompleteHandler _handler;
    private readonly ObjectId _orderId = ObjectId.GenerateNewId();
    private readonly ObjectId _shopId = ObjectId.GenerateNewId();

    public PayAndCompleteHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _inventoryServiceMock = new Mock<IInventoryService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<PayAndCompleteHandler>>();
        _handler = new PayAndCompleteHandler(_orderRepositoryMock.Object, _inventoryServiceMock.Object, _unitOfWorkMock.Object, _loggerMock.Object);
    }

    private Order CreateValidOrder(OrderStatus status = OrderStatus.Ready, PaymentStatus paymentStatus = PaymentStatus.Pending)
    {
        var customerInfo = new CustomerInfo("Test Customer", "0123456789", CustomerType.DineIn);
        var items = new List<OrderItem>
        {
            new OrderItem(ObjectId.GenerateNewId(), "Test Latte", "A delicious test latte", new Money(50000, "VND"), 2)
        };
        var order = new Order(_orderId, _shopId, ObjectId.GenerateNewId(), customerInfo, items, DateTime.UtcNow);
        // Use reflection to set status and payment status for testing
        typeof(Order).GetProperty("Status")!.SetValue(order, status);
        typeof(Order).GetProperty("PaymentStatus")!.SetValue(order, paymentStatus);
        return order;
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsSuccessAndCallsRepository()
    {
        // Arrange
        var order = CreateValidOrder();
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Setup inventory service to return success for inventory lookups and adjustments
        var inventoryItemId = ObjectId.GenerateNewId();
        var inventoryItem = Domain.Inventory.InventoryItem.Create(
            _shopId, "Test Latte", "cup", 10, 2).Value;
        // Use reflection to set the Id since it's read-only
        typeof(Domain.Inventory.InventoryItem).GetProperty("Id")!.SetValue(inventoryItem, inventoryItemId);

        _inventoryServiceMock.Setup(x => x.GetByShopIdAndNameAsync(_shopId, "Test Latte"))
            .ReturnsAsync(Result.Ok(inventoryItem));
        _inventoryServiceMock.Setup(x => x.AdjustQuantityAsync(inventoryItemId, -2, $"Order {order.OrderNumber} completed", order.StaffId))
            .ReturnsAsync(Result.Ok());

        var command = new PayAndCompleteCommand(_orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _orderRepositoryMock.Verify(x => x.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _inventoryServiceMock.Verify(x => x.GetByShopIdAndNameAsync(_shopId, "Test Latte"), Times.Once);
        _inventoryServiceMock.Verify(x => x.AdjustQuantityAsync(inventoryItemId, -2, $"Order {order.OrderNumber} completed", order.StaffId), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFailure()
    {
        // Arrange
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new PayAndCompleteCommand(_orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("not found"));
    }

    [Fact]
    public async Task Handle_OrderNotReady_ReturnsFailure()
    {
        // Arrange
        var order = CreateValidOrder(OrderStatus.Placed); // Not Ready
        _orderRepositoryMock.Setup(x => x.GetByIdAsync(_orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new PayAndCompleteCommand(_orderId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot transition"));
    }
}

public class GetLowStockItemsHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly GetLowStockItemsHandler _handler;
    private readonly ObjectId _shopId = ObjectId.GenerateNewId();

    public GetLowStockItemsHandlerTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _handler = new GetLowStockItemsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithShopId_ReturnsLowStockItemsForShop()
    {
        // Arrange
        var items = new List<Domain.Inventory.InventoryItem>
        {
            CreateItem("Coffee Beans", 1.5m, 2.0m), // Low stock
            CreateItem("Milk", 0.5m, 1.0m) // Low stock
        };
        _repositoryMock.Setup(x => x.GetLowStockItemsAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var query = new GetLowStockItemsQuery(_shopId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        _repositoryMock.Verify(x => x.GetLowStockItemsAsync(_shopId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutShopId_ReturnsLowStockItemsForAllShops()
    {
        // Arrange
        var items = new List<Domain.Inventory.InventoryItem>
        {
            CreateItem("Coffee Beans", 1.5m, 2.0m)
        };
        _repositoryMock.Setup(x => x.GetLowStockItemsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var query = new GetLowStockItemsQuery(null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        _repositoryMock.Verify(x => x.GetLowStockItemsAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoLowStockItems_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetLowStockItemsAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Inventory.InventoryItem>());

        var query = new GetLowStockItemsQuery(_shopId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private static Domain.Inventory.InventoryItem CreateItem(string name, decimal quantity, decimal reorderLevel)
    {
        var item = Domain.Inventory.InventoryItem.Create(
            ObjectId.GenerateNewId(), name, "kg", quantity, reorderLevel).Value;
        return item;
    }
}

public class GetOutOfStockItemsHandlerTests
{
    private readonly Mock<IInventoryItemRepository> _repositoryMock;
    private readonly GetOutOfStockItemsHandler _handler;
    private readonly ObjectId _shopId = ObjectId.GenerateNewId();

    public GetOutOfStockItemsHandlerTests()
    {
        _repositoryMock = new Mock<IInventoryItemRepository>();
        _handler = new GetOutOfStockItemsHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithShopId_ReturnsOutOfStockItemsForShop()
    {
        // Arrange
        var items = new List<Domain.Inventory.InventoryItem>
        {
            CreateItem("Coffee Beans", 0m),
            CreateItem("Milk", -1m) // Negative due to adjustment
        };
        _repositoryMock.Setup(x => x.GetOutOfStockItemsAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var query = new GetOutOfStockItemsQuery(_shopId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_NoOutOfStockItems_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetOutOfStockItemsAsync(_shopId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Inventory.InventoryItem>());

        var query = new GetOutOfStockItemsQuery(_shopId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    private static Domain.Inventory.InventoryItem CreateItem(string name, decimal quantity)
    {
        // InventoryItem requires quantity >= 0, so clamp to 0 for out-of-stock scenarios
        var validQuantity = quantity < 0 ? 0m : quantity;
        var item = Domain.Inventory.InventoryItem.Create(
            ObjectId.GenerateNewId(), name, "kg", validQuantity, 1.0m).Value;
        return item;
    }
}