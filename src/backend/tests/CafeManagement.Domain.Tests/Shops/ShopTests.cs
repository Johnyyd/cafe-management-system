using CafeManagement.Domain.Shops;
using CafeManagement.Domain.Shared;
using Xunit;

namespace CafeManagement.Domain.Tests.Shops;

public class ShopTests
{
    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var address = new Address("123 Main St", "Hanoi", "Hoan Kiem", "100000");
        var contact = new ContactInfo("0123456789", "test@example.com");
        var operatingHours = new List<OperatingHours>
        {
            new OperatingHours(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0)), // Sunday
            new OperatingHours(1, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))  // Monday
        };

        // Act
        var result = Shop.Create("Test Cafe", address, contact, operatingHours);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Test Cafe", result.Value.Name);
        Assert.Equal(address, result.Value.Address);
        Assert.Equal(contact, result.Value.Contact);
        Assert.Equal(2, result.Value.OperatingHours.Count);
        Assert.Equal(ShopStatus.Active, result.Value.Status);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Create_NullName_ReturnsFailure()
    {
        // Arrange
        var address = new Address("123 Main St", "Hanoi", "Hoan Kiem", "100000");
        var contact = new ContactInfo("0123456789", "test@example.com");
        var operatingHours = new List<OperatingHours>
        {
            new OperatingHours(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
        };

        // Act
        var result = Shop.Create(null!, address, contact, operatingHours);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Name"));
    }

    [Fact]
    public void Update_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var address = new Address("123 Main St", "Hanoi", "Hoan Kiem", "100000");
        var contact = new ContactInfo("0123456789", "test@example.com");
        var operatingHours = new List<OperatingHours>
        {
            new OperatingHours(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
        };
        var shop = Shop.Create("Old Name", address, contact, operatingHours).Value;

        // Act
        var result = shop.Update("New Name", address, contact);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", shop.Name);
        Assert.Equal(address, shop.Address);
        Assert.Equal(contact, shop.Contact);
    }

    [Fact]
    public void Update_NullName_ReturnsFailure()
    {
        // Arrange
        var address = new Address("123 Main St", "Hanoi", "Hoan Kiem", "100000");
        var contact = new ContactInfo("0123456789", "test@example.com");
        var operatingHours = new List<OperatingHours>
        {
            new OperatingHours(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
        };
        var shop = Shop.Create("Old Name", address, contact, operatingHours).Value;

        // Act
        var result = shop.Update(null!, address, contact);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Name"));
    }

    [Fact]
    public void Deactivate_SetsDeletedAt()
    {
        // Arrange
        var address = new Address("123 Main St", "Hanoi", "Hoan Kiem", "100000");
        var contact = new ContactInfo("0123456789", "test@example.com");
        var operatingHours = new List<OperatingHours>
        {
            new OperatingHours(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
        };
        var shop = Shop.Create("Test Cafe", address, contact, operatingHours).Value;

        // Act
        var result = shop.Deactivate();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(shop.IsDeleted);
        Assert.NotNull(shop.DeletedAt);
    }
}