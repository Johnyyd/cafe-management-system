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

    [Fact]
    public void AssignToShop_PreventsDuplicateAssignment_WhenSameShopAndStatus()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate).Value;

        // Assign to shop first time
        var result1 = staff.AssignToShop(shopId, true);
        Assert.True(result1.IsSuccess);

        // Act - Try to assign to same shop with same primary status
        var result2 = staff.AssignToShop(shopId, true);

        // Assert - Should succeed but indicate no change (already assigned correctly)
        Assert.True(result2.IsSuccess);
        Assert.Contains(shopId, staff.GetAssignedShopIds());
        Assert.Equal(shopId, staff.GetPrimaryShopId());
        // Should not create duplicate assignment
        var assignments = staff.ShopAssignments.Where(a => a.ShopId == shopId && a.IsActive).ToList();
        Assert.Single(assignments);
    }

    [Fact]
    public void AssignToShop_UpdatesPrimaryStatus_WhenSameShopDifferentStatus()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate).Value;

        // Assign to shop as not primary first
        var result1 = staff.AssignToShop(shopId, false);
        Assert.True(result1.IsSuccess);
        Assert.False(staff.GetPrimaryShopId() == shopId); // Should not be primary yet

        // Act - Assign to same shop as primary
        var result2 = staff.AssignToShop(shopId, true);

        // Assert - Should succeed and update primary status
        Assert.True(result2.IsSuccess);
        Assert.Contains(shopId, staff.GetAssignedShopIds());
        Assert.Equal(shopId, staff.GetPrimaryShopId()); // Should now be primary
        // Should not create duplicate assignment
        var assignments = staff.ShopAssignments.Where(a => a.ShopId == shopId && a.IsActive).ToList();
        Assert.Single(assignments);
    }

    [Fact]
    public void AssignToShop_PreventsMultiplePrimaryAssignments()
    {
        // Arrange
        var contact = new ContactInfo("0123456789", "test@example.com");
        var hireDate = new DateTime(2023, 1, 15);
        var shopId1 = ObjectId.GenerateNewId();
        var shopId2 = ObjectId.GenerateNewId();
        var staff = CafeManagement.Domain.Staff.Staff.Hire("John", "Doe", StaffRole.Barista, contact, hireDate).Value;

        // Assign to first shop as primary
        var result1 = staff.AssignToShop(shopId1, true);
        Assert.True(result1.IsSuccess);
        Assert.Equal(shopId1, staff.GetPrimaryShopId());

        // Assign to second shop as primary
        var result2 = staff.AssignToShop(shopId2, true);
        Assert.True(result2.IsSuccess);

        // Assert - First shop should no longer be primary, second shop should be primary
        Assert.Equal(shopId2, staff.GetPrimaryShopId());
        Assert.Contains(shopId1, staff.GetAssignedShopIds());
        Assert.Contains(shopId2, staff.GetAssignedShopIds());

        // Verify only one primary assignment exists
        var primaryAssignments = staff.ShopAssignments.Where(a => a.IsActive && a.IsPrimary).ToList();
        Assert.Single(primaryAssignments);
        Assert.Equal(shopId2, primaryAssignments[0].ShopId);
    }
}