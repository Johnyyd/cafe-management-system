using CafeManagement.Domain.Shared;
using Xunit;

namespace CafeManagement.Domain.Tests.Shared;

public class MoneyTests
{
    [Fact]
    public void Create_ValidAmount_ReturnsMoney()
    {
        // Act
        var money = new Money(123.45m);

        // Assert
        Assert.Equal(123.45m, money.Amount);
        Assert.Equal("VND", money.Currency);
    }

    [Fact]
    public void Create_NegativeAmount_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Money(-10m));
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        // Arrange
        var money1 = new Money(100m);
        var money2 = new Money(50m);

        // Act
        var result = money1.Add(money2);

        // Assert
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public void Add_DifferentCurrency_ThrowsException()
    {
        // Arrange
        var money1 = new Money(100m, "VND");
        var money2 = new Money(50m, "USD");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1.Add(money2));
    }

    [Fact]
    public void Multiply_ByQuantity_ReturnsProduct()
    {
        // Arrange
        var money = new Money(25.50m);

        // Act
        var result = money.Multiply(3);

        // Assert
        Assert.Equal(76.50m, result.Amount);
    }
}