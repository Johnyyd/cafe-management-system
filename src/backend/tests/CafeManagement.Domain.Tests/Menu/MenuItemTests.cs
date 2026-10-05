using CafeManagement.Domain.Menu;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.Domain.Tests.Menu;

public class MenuItemTests
{
    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var price = new Money(25.50m);
        var availability = new Availability(
            new TimeSpan(6, 0, 0), // 6:00 AM
            new TimeSpan(22, 0, 0), // 10:00 PM
            new List<int> { 0, 1, 2, 3, 4 } // Weekdays
        );

        // Act
        var result = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            price,
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(shopId, result.Value.ShopId);
        Assert.Equal("Beverage", result.Value.Category);
        Assert.Equal("Latte", result.Value.Name);
        Assert.Equal("Espresso with steamed milk", result.Value.Description);
        Assert.Equal(price, result.Value.Price);
        Assert.Contains("Espresso", result.Value.Ingredients);
        Assert.Contains("Milk", result.Value.Ingredients);
        Assert.Contains("Dairy", result.Value.Allergens);
        Assert.Equal(availability, result.Value.Availability);
        Assert.Equal(MenuItemStatus.Available, result.Value.Status);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Create_InvalidPrice_ReturnsFailure()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var availability = new Availability(
            new TimeSpan(6, 0, 0),
            new TimeSpan(22, 0, 0),
            new List<int> { 0, 1, 2, 3, 4 }
        );

        // Act - Pass a zero price (valid Money constructor, fails domain validation)
        var result = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            Money.Zero(), // Zero price should fail domain validation
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Price") || e.Message.Contains("OutOfRange"));
    }

    [Fact]
    public void Update_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var price = new Money(25.50m);
        var availability = new Availability(
            new TimeSpan(6, 0, 0),
            new TimeSpan(22, 0, 0),
            new List<int> { 0, 1, 2, 3, 4 }
        );
        var menuItem = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            price,
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        ).Value;

        // Act
        var result = menuItem.Update(
            "Food",
            "Sandwich",
            "Ham and cheese sandwich",
            new Money(35.00m),
            new List<string> { "Bread", "Ham", "Cheese" },
            new List<string> { "Gluten", "Dairy" },
            new Availability(
                new TimeSpan(7, 0, 0),
                new TimeSpan(21, 0, 0),
                new List<int> { 0, 1, 2, 3, 4 }
            )
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Food", menuItem.Category);
        Assert.Equal("Sandwich", menuItem.Name);
        Assert.Equal("Ham and cheese sandwich", menuItem.Description);
        Assert.Equal(new Money(35.00m), menuItem.Price);
        Assert.Contains("Bread", menuItem.Ingredients);
        Assert.Contains("Ham", menuItem.Ingredients);
        Assert.Contains("Cheese", menuItem.Ingredients);
        Assert.Contains("Gluten", menuItem.Allergens);
        Assert.Contains("Dairy", menuItem.Allergens);
    }

    [Fact]
    public void UpdateAvailability_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var price = new Money(25.50m);
        var availability = new Availability(
            new TimeSpan(6, 0, 0), // 6:00 AM
            new TimeSpan(22, 0, 0), // 10:00 PM
            new List<int> { 0, 1, 2, 3, 4 } // Weekdays
        );
        var menuItem = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            price,
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        ).Value;

        var newAvailability = new Availability(
            new TimeSpan(8, 0, 0),
            new TimeSpan(23, 0, 0),
            new List<int> { 5, 6 }
        );

        // Act
        var result = menuItem.UpdateAvailability(newAvailability);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newAvailability, menuItem.Availability);
    }

    [Fact]
    public void SetStatus_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var price = new Money(25.50m);
        var availability = new Availability(
            new TimeSpan(6, 0, 0),
            new TimeSpan(22, 0, 0),
            new List<int> { 0, 1, 2, 3, 4 }
        );
        var menuItem = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            price,
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        ).Value;

        // Act
        var result = menuItem.SetStatus(MenuItemStatus.Unavailable);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(MenuItemStatus.Unavailable, menuItem.Status);
    }

    [Fact]
    public void IsAvailableAt_ReturnsCorrectResult()
    {
        // Arrange
        var shopId = ObjectId.GenerateNewId();
        var price = new Money(25.50m);
        var availability = new Availability(
            new TimeSpan(7, 0, 0),
            new TimeSpan(20, 0, 0),
            new List<int> { 1, 2, 3, 4, 5 }
        );
        var menuItem = MenuItem.Create(
            shopId,
            "Beverage",
            "Latte",
            "Espresso with steamed milk",
            price,
            new List<string> { "Espresso", "Milk" },
            new List<string> { "Dairy" },
            availability
        ).Value;

        // Act & Assert
        // Monday at 10:00 AM - should be available
        Assert.True(menuItem.IsAvailableAt(new DateTime(2023, 7, 31, 10, 0, 0))); // Monday

        // Sunday at 10:00 AM - should not be available (Sunday not in days)
        Assert.False(menuItem.IsAvailableAt(new DateTime(2023, 8, 6, 10, 0, 0))); // Sunday

        // Monday at 6:00 AM - should not be available (before opening)
        Assert.False(menuItem.IsAvailableAt(new DateTime(2023, 7, 31, 6, 0, 0)));

        // Monday at 9:00 PM - should not be available (after closing)
        Assert.False(menuItem.IsAvailableAt(new DateTime(2023, 7, 31, 21, 0, 0)));

        // Deactivate item - should not be available
        menuItem.Deactivate();
        Assert.False(menuItem.IsAvailableAt(new DateTime(2023, 7, 31, 10, 0, 0)));
    }
}