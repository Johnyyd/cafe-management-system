using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CafeManagement.Api;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Domain.Staff;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class StaffAssignmentApiTests
{
    private readonly HttpClient _client;

    public StaffAssignmentApiTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    private async Task<string> CreateShopAsync(string name = "Test Cafe")
    {
        var createShopDto = new CreateShopCommand(
            name,
            new AddressDto("123 Test Street", "Hanoi", "Ba Dinh", "100000"),
            new ContactInfoDto("0123456789", "test@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0)),
                new OperatingHoursDto(1, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
            }
        );

        var response = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var shopId = await response.Content.ReadFromJsonAsync<string>();
        return shopId!;
    }

    private async Task<string> CreateAdminAndLoginAsync(string shopId, string email = "admin@test.com")
    {
        // Create an admin staff user
        var createStaffDto = new CreateStaffCommand(
            "Admin",
            "User",
            StaffRole.Admin,
            new ContactInfoDto("0111111111", email),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var staffResponse = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        staffResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Register a user account for this admin (email must match)
        var registerDto = new
        {
            email,
            password = "Password123",
            fullName = "Admin User",
            role = "Admin"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        return authResult!.AccessToken;
    }

    private async Task<string> CreateStaffAndLoginAsync(string shopId, string email, StaffRole role = StaffRole.Cashier)
    {
        var createStaffDto = new CreateStaffCommand(
            "Test",
            "Staff",
            role,
            new ContactInfoDto("0987654321", email),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var staffResponse = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        staffResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerDto = new
        {
            email,
            password = "Password123",
            fullName = "Test Staff",
            role = role.ToString()
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        return authResult!.AccessToken;
    }

    private async Task<string> CreateStaffAsync(string shopId, string adminToken, string email, StaffRole role = StaffRole.Cashier)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createStaffDto = new CreateStaffCommand(
            "Test",
            "Staff",
            role,
            new ContactInfoDto("0987654321", email),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var response = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var staffId = await response.Content.ReadFromJsonAsync<string>();
        return staffId!;
    }

    [Fact]
    public async Task AssignStaffToShop_ValidInput_ReturnsNoContent()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff1@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), false);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify assignment
        var getResponse = await _client.GetAsync($"/api/v1/staff/{staffId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staff = await getResponse.Content.ReadFromJsonAsync<StaffDto>();
        staff.Should().NotBeNull();
        staff!.ShopAssignments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AssignStaffToShop_AsPrimary_SetsPrimaryShop()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff2@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - Assign as primary
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), true);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify primary assignment
        var getResponse = await _client.GetAsync($"/api/v1/staff/{staffId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staff = await getResponse.Content.ReadFromJsonAsync<StaffDto>();
        staff.Should().NotBeNull();
        staff!.PrimaryShopId.Should().Be(shopId);
    }

    [Fact]
    public async Task AssignStaffToShop_DuplicateAssignment_ReturnsNoContent()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff3@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // First assignment
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), false);
        var response1 = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);
        response1.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Second assignment to same shop
        var response2 = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert - Should succeed (idempotent)
        response2.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnassignStaffFromShop_ValidInput_ReturnsNoContent()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff4@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // First assign
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), true);
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Act - Unassign
        var unassignDto = new UnassignStaffFromShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!));
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/unassign-shop", unassignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify unassignment
        var getResponse = await _client.GetAsync($"/api/v1/staff/{staffId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staff = await getResponse.Content.ReadFromJsonAsync<StaffDto>();
        staff.Should().NotBeNull();
        staff!.PrimaryShopId.Should().BeNull();
    }

    [Fact]
    public async Task SetStaffPrimaryShop_ValidInput_ReturnsNoContent()
    {
        // Arrange
        var shopId1 = await CreateShopAsync("Shop 1");
        var shopId2 = await CreateShopAsync("Shop 2");
        var adminToken = await CreateAdminAndLoginAsync(shopId1);
        var staffId = await CreateStaffAsync(shopId1, adminToken, "staff5@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Assign to both shops (not primary)
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId1!), false));
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!), false));

        // Act - Set shop 2 as primary
        var setPrimaryDto = new SetStaffPrimaryShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!));
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/set-primary-shop", setPrimaryDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify primary shop changed
        var getResponse = await _client.GetAsync($"/api/v1/staff/{staffId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staff = await getResponse.Content.ReadFromJsonAsync<StaffDto>();
        staff.Should().NotBeNull();
        staff!.PrimaryShopId.Should().Be(shopId2);
    }

    [Fact]
    public async Task SetStaffPrimaryShop_UnsetsOtherPrimary()
    {
        // Arrange
        var shopId1 = await CreateShopAsync("Shop 1");
        var shopId2 = await CreateShopAsync("Shop 2");
        var adminToken = await CreateAdminAndLoginAsync(shopId1);
        var staffId = await CreateStaffAsync(shopId1, adminToken, "staff6@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Assign to both shops, shop 1 as primary
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId1!), true));
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!), false));

        // Verify shop 1 is primary
        var getResponse1 = await _client.GetAsync($"/api/v1/staff/{staffId}");
        var staff1 = await getResponse1.Content.ReadFromJsonAsync<StaffDto>();
        staff1!.PrimaryShopId.Should().Be(shopId1);

        // Act - Set shop 2 as primary
        var setPrimaryDto = new SetStaffPrimaryShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!));
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/set-primary-shop", setPrimaryDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify shop 2 is now primary and shop 1 is not
        var getResponse2 = await _client.GetAsync($"/api/v1/staff/{staffId}");
        var staff2 = await getResponse2.Content.ReadFromJsonAsync<StaffDto>();
        staff2.Should().NotBeNull();
        staff2!.PrimaryShopId.Should().Be(shopId2);

        // Verify shop 1 assignment still exists but not primary
        staff2.ShopAssignments.Should().Contain(a => a.ShopId == shopId1 && a.IsPrimary == false);
        staff2.ShopAssignments.Should().Contain(a => a.ShopId == shopId2 && a.IsPrimary == true);
    }

    [Fact]
    public async Task AssignStaffToShop_WithoutAdminRole_ReturnsForbidden()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff7@test.com");

        // Create a non-admin user (cashier)
        var cashierToken = await CreateStaffAndLoginAsync(shopId, "cashier@test.com", StaffRole.Cashier);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        // Act
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), false);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignStaffToShop_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff8@test.com");

        _client.DefaultRequestHeaders.Authorization = null; // No auth

        // Act
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId!), false);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignStaffToShop_NonExistentStaff_ReturnsNotFound()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var nonExistentStaffId = ObjectId.GenerateNewId().ToString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(nonExistentStaffId), ObjectId.Parse(shopId!), false);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{nonExistentStaffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignStaffToShop_NonExistentShop_ReturnsNotFound()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        var staffId = await CreateStaffAsync(shopId, adminToken, "staff9@test.com");
        var nonExistentShopId = ObjectId.GenerateNewId().ToString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var assignDto = new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(nonExistentShopId), false);
        var response = await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop", assignDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignStaffToShop_MultipleShops_OnlyOnePrimary()
    {
        // Arrange
        var shopId1 = await CreateShopAsync("Shop 1");
        var shopId2 = await CreateShopAsync("Shop 2");
        var shopId3 = await CreateShopAsync("Shop 3");
        var adminToken = await CreateAdminAndLoginAsync(shopId1);
        var staffId = await CreateStaffAsync(shopId1, adminToken, "staff10@test.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Assign to all three shops, none as primary initially
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId1!), false));
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!), false));
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/assign-shop",
            new AssignStaffToShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId3!), false));

        // Set shop 2 as primary
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/set-primary-shop",
            new SetStaffPrimaryShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId2!)));

        // Set shop 3 as primary (should unset shop 2)
        await _client.PostAsJsonAsync($"/api/v1/staff/{staffId}/set-primary-shop",
            new SetStaffPrimaryShopCommand(ObjectId.Parse(staffId!), ObjectId.Parse(shopId3!)));

        // Verify only shop 3 is primary
        var getResponse = await _client.GetAsync($"/api/v1/staff/{staffId}");
        var staff = await getResponse.Content.ReadFromJsonAsync<StaffDto>();
        staff.Should().NotBeNull();
        staff!.PrimaryShopId.Should().Be(shopId3);

        var primaryAssignments = staff.ShopAssignments.Where(a => a.IsActive && a.IsPrimary).ToList();
        primaryAssignments.Should().HaveCount(1);
        primaryAssignments[0].ShopId.Should().Be(shopId3);
    }
}