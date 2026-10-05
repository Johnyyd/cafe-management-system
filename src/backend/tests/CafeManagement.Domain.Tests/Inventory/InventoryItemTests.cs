using CafeManagement.Domain.Inventory;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.Domain.Tests.Inventory;

public class InventoryItemTests
{
    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();

        // Act
        var result = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            10.5m,
            2.0m
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(shopId, result.Value.ShopId);
        Assert.Equal("Coffee Beans", result.Value.ItemName);
        Assert.Equal("kg", result.Value.Unit);
        Assert.Equal(10.5m, result.Value.Quantity);
        Assert.Equal(2.0m, result.Value.ReorderLevel);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Create_NegativeQuantity_ReturnsFailure()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();

        // Act
        var result = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            -5.0m, // Invalid negative quantity
            2.0m
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Quantity"));
    }

    [Fact]
    public void Create_NegativeReorderLevel_ReturnsFailure()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();

        // Act
        var result = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            10.5m,
            -1.0m // Invalid negative reorder level
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("ReorderLevel"));
    }

    [Fact]
    public void Update_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            10.5m,
            2.0m
        ).Value;

        // Act
        var result = inventoryItem.Update(
            "Espresso Beans",
            "g",
            5.0m,
            null // No supplier
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Espresso Beans", inventoryItem.ItemName);
        Assert.Equal("g", inventoryItem.Unit);
        Assert.Equal(5.0m, inventoryItem.ReorderLevel);
        Assert.Null(inventoryItem.SupplierId);
    }

    [Fact]
    public void AdjustQuantity_Positive_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            10.0m,
            2.0m
        ).Value;

        // Act
        var result = inventoryItem.AdjustQuantity(5.0m, "Restocked");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(15.0m, inventoryItem.Quantity);
    }

    [Fact]
    public void AdjustQuantity_Negative_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            3.0m,
            2.0m
        ).Value;

        // Act
        var result = inventoryItem.AdjustQuantity(-5.0m, "Used in drinks"); // Trying to use more than available

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Insufficient stock") || e.Message.Contains("InsufficientStock") || e.Metadata.ContainsKey("Code") && e.Metadata["Code"].ToString() == "Business.InsufficientStock");
    }

    [Fact]
    public void SetQuantity_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            10.0m,
            2.0m
        ).Value;

        // Act
        var result = inventoryItem.SetQuantity(25.0m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(25.0m, inventoryItem.Quantity);
    }

    [Fact]
    public void IsLowStock_ReturnsCorrectResult()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            1.5m, // Below reorder level
            2.0m
        ).Value;

        // Act & Assert
        Assert.True(inventoryItem.IsLowStock());

        // Test when quantity is above reorder level
        inventoryItem.SetQuantity(3.0m);
        Assert.False(inventoryItem.IsLowStock());

        // Test when reorder level is 0 (should never be low stock)
        var inventoryItem2 = InventoryItem.Create(
            shopId,
            "Milk",
            "liters",
            5.0m,
            0.0m // No reorder level
        ).Value;

        Assert.False(inventoryItem2.IsLowStock()); // Should be false when reorder level is 0
    }

    [Fact]
    public void IsOutOfStock_ReturnsCorrectResult()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var inventoryItem = InventoryItem.Create(
            shopId,
            "Coffee Beans",
            "kg",
            0.0m, // Out of stock
            2.0m
        ).Value;

        // Act & Assert
        Assert.True(inventoryItem.IsOutOfStock());

        // Test when in stock
        inventoryItem.SetQuantity(5.0m);
        Assert.False(inventoryItem.IsOutOfStock());
    }
}