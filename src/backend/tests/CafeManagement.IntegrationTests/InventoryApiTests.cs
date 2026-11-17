using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CafeManagement.Api;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Inventory.Commands;
using CafeManagement.Application.Inventory.Queries;
using CafeManagement.Application.Inventory.Responses;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Domain.Staff;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class InventoryApiTests
{
    private readonly HttpClient _client;

    public InventoryApiTests()
    {
        _client = IntegrationTestFixture.Client;
    }

    private async Task<string> CreateShopAsync()
    {
        var createShopDto = new CreateShopCommand(
            "Test Cafe",
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

    private async Task<string> CreateAdminAndLoginAsync(string shopId)
    {
        // Create an admin staff user
        var createStaffDto = new CreateStaffCommand(
            "Admin",
            "User",
            StaffRole.Admin,
            new ContactInfoDto("0111111111", "admin@test.com"),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var staffResponse = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        staffResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Register a user account for this admin (email must match)
        var registerDto = new
        {
            email = "admin@test.com",
            password = "Password123",
            fullName = "Admin User",
            role = "Admin"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        return authResult!.AccessToken;
    }

    private async Task<string> CreateInventoryItemAsync(string shopId, string adminToken, string itemName, decimal quantity, decimal reorderLevel)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDto = new CreateInventoryItemCommand(
            ObjectId.Parse(shopId),
            itemName,
            "kg",
            quantity,
            reorderLevel,
            null, // SupplierId
            ObjectId.GenerateNewId() // CreatedBy
        );

        var response = await _client.PostAsJsonAsync("/api/v1/inventory", createDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var item = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();
        return item!.Id.ToString();
    }

    [Fact]
    public async Task GetLowStockItems_WithShopId_ReturnsLowStockItemsForShop()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);

        // Create items - some low stock, some not
        await CreateInventoryItemAsync(shopId, adminToken, "Coffee Beans", 1.5m, 2.0m); // Low stock (1.5 < 2.0)
        await CreateInventoryItemAsync(shopId, adminToken, "Milk", 0.5m, 1.0m); // Low stock (0.5 < 1.0)
        await CreateInventoryItemAsync(shopId, adminToken, "Sugar", 5.0m, 1.0m); // Not low stock (5.0 >= 1.0)

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/low-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().HaveCount(2);
        items.Should().AllSatisfy(item => item.IsLowStock.Should().BeTrue());
        items.Should().Contain(i => i.ItemName == "Coffee Beans");
        items.Should().Contain(i => i.ItemName == "Milk");
    }

    [Fact]
    public async Task GetLowStockItems_WithoutShopId_ReturnsLowStockItemsForAllShops()
    {
        // Arrange
        var shopId1 = await CreateShopAsync();
        var shopId2 = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId1);

        // Create low stock item in shop 1
        await CreateInventoryItemAsync(shopId1, adminToken, "Coffee Beans Shop 1", 1.0m, 2.0m);

        // Create low stock item in shop 2
        await CreateInventoryItemAsync(shopId2, adminToken, "Milk Shop 2", 0.5m, 1.0m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - No shopId parameter
        var response = await _client.GetAsync("/api/v1/inventory/low-stock");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterOrEqualTo(2);
        items.Should().AllSatisfy(item => item.IsLowStock.Should().BeTrue());
    }

    [Fact]
    public async Task GetLowStockItems_NoLowStockItems_ReturnsEmptyList()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);

        // Create items with sufficient stock
        await CreateInventoryItemAsync(shopId, adminToken, "Sugar", 10.0m, 1.0m);
        await CreateInventoryItemAsync(shopId, adminToken, "Water", 5.0m, 1.0m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/low-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOutOfStockItems_WithShopId_ReturnsOutOfStockItemsForShop()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);

        // Create items - some out of stock, some not
        await CreateInventoryItemAsync(shopId, adminToken, "Coffee Beans", 0m, 2.0m); // Out of stock (0)
        await CreateInventoryItemAsync(shopId, adminToken, "Milk", -1m, 1.0m); // Out of stock (negative)
        await CreateInventoryItemAsync(shopId, adminToken, "Sugar", 5.0m, 1.0m); // Not out of stock

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/out-of-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().HaveCount(2);
        items.Should().AllSatisfy(item => item.IsOutOfStock.Should().BeTrue());
        items.Should().Contain(i => i.ItemName == "Coffee Beans");
        items.Should().Contain(i => i.ItemName == "Milk");
    }

    [Fact]
    public async Task GetOutOfStockItems_WithoutShopId_ReturnsOutOfStockItemsForAllShops()
    {
        // Arrange
        var shopId1 = await CreateShopAsync();
        var shopId2 = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId1);

        // Create out of stock item in shop 1
        await CreateInventoryItemAsync(shopId1, adminToken, "Coffee Beans Shop 1", 0m, 2.0m);

        // Create out of stock item in shop 2
        await CreateInventoryItemAsync(shopId2, adminToken, "Milk Shop 2", 0m, 1.0m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - No shopId parameter
        var response = await _client.GetAsync("/api/v1/inventory/out-of-stock");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().HaveCountGreaterOrEqualTo(2);
        items.Should().AllSatisfy(item => item.IsOutOfStock.Should().BeTrue());
    }

    [Fact]
    public async Task GetOutOfStockItems_NoOutOfStockItems_ReturnsEmptyList()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);

        // Create items with stock
        await CreateInventoryItemAsync(shopId, adminToken, "Sugar", 10.0m, 1.0m);
        await CreateInventoryItemAsync(shopId, adminToken, "Water", 5.0m, 1.0m);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/out-of-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<InventoryItemResponse>>();
        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockItems_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var shopId = await CreateShopAsync();

        _client.DefaultRequestHeaders.Authorization = null; // No auth

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/low-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetOutOfStockItems_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var shopId = await CreateShopAsync();

        _client.DefaultRequestHeaders.Authorization = null; // No auth

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/out-of-stock?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLowStockItems_WithNonAdminRole_ReturnsForbidden()
    {
        // Arrange
        var shopId = await CreateShopAsync();

        // Create a cashier staff user (not admin/manager)
        var createStaffDto = new CreateStaffCommand(
            "Cashier",
            "User",
            StaffRole.Cashier,
            new ContactInfoDto("0222222222", "cashier@test.com"),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var staffResponse = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        staffResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Register user account
        var registerDto = new
        {
            email = "cashier@test.com",
            password = "Password123",
            fullName = "Cashier User",
            role = "Cashier"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        var cashierToken = authResult!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/low-stock?shopId={shopId}");

        // Assert - Cashier doesn't have permission to view inventory
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOutOfStockItems_WithNonAdminRole_ReturnsForbidden()
    {
        // Arrange
        var shopId = await CreateShopAsync();

        // Create a cashier staff user (not admin/manager)
        var createStaffDto = new CreateStaffCommand(
            "Cashier",
            "User",
            StaffRole.Cashier,
            new ContactInfoDto("0333333333", "cashier2@test.com"),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var staffResponse = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        staffResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Register user account
        var registerDto = new
        {
            email = "cashier2@test.com",
            password = "Password123",
            fullName = "Cashier User 2",
            role = "Cashier"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        var cashierToken = authResult!.AccessToken;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", cashierToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/inventory/out-of-stock?shopId={shopId}");

        // Assert - Cashier doesn't have permission to view inventory
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}