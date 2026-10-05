using CafeManagement.Domain.Staff;
using CafeManagement.Domain.Shared;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.Domain.Tests.Staff;

public class StaffTests
{
    [Fact]
    public void Create_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);

        // Act
        var result = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("John", result.Value.FirstName);
        Assert.Equal("Doe", result.Value.LastName);
        Assert.Equal(StaffRole.Barista, result.Value.Role);
        Assert.Equal(contact, result.Value.Contact);
        Assert.Equal(hireDate, result.Value.HireDate);
        Assert.Equal(EmploymentStatus.Active, result.Value.EmploymentStatus);
        Assert.False(result.Value.IsDeleted);
    }

    [Fact]
    public void Hire_NullFirstName_ReturnsFailure()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();

        // Act
        var result = CafeManagement.Domain.Staff.Staff.Hire(null!, "Doe", StaffRole.Barista, contact, hireDate);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("FirstName"));
    }

    [Fact]
    public void Hire_InvalidRole_ReturnsFailure()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();

        // Act
        var result = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", (StaffRole)999, contact, hireDate);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Role"));
    }

    [Fact]
    public void Update_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate).Value;

        // Act
        var result = staff.Update("Johnny", "Smith", new ContactInfo("0987654321", "johnny@example.com"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Johnny", staff.FirstName);
        Assert.Equal("Smith", staff.LastName);
        Assert.Equal("0987654321", staff.Contact.Phone);
        Assert.Equal("johnny@example.com", staff.Contact.Email);
    }

    [Fact]
    public void ChangeRole_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate, shopId).Value;

        // Act
        var result = staff.ChangeRole(StaffRole.Manager);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(StaffRole.Manager, staff.Role);
    }

    [Fact]
    public void ChangeEmploymentStatus_ToTerminated_ReturnsSuccess()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate, shopId).Value;

        // Act
        var result = staff.ChangeEmploymentStatus(EmploymentStatus.Terminated);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(EmploymentStatus.Terminated, staff.EmploymentStatus);
    }

    [Fact]
    public void AssignToShop_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();
        var newShopId = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate).Value;

        // Act
        var result = staff.AssignToShop(shopId, true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains(shopId, staff.GetAssignedShopIds());
        Assert.Equal(shopId, staff.GetPrimaryShopId());

        // Act - Assign to another shop
        result = staff.AssignToShop(newShopId, false);
        Assert.True(result.IsSuccess);
        Assert.Contains(newShopId, staff.GetAssignedShopIds());
        Assert.Equal(shopId, staff.GetPrimaryShopId()); // First shop remains primary

        // Act - Set new shop as primary
        result = staff.SetPrimaryShop(newShopId);
        Assert.True(result.IsSuccess);
        Assert.Equal(newShopId, staff.GetPrimaryShopId());
    }
}