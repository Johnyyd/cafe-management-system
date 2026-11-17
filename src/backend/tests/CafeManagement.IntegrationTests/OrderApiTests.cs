using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CafeManagement.Api;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Menu.Commands;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Orders.Queries;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Staff;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class OrderApiTests
{
    private readonly HttpClient _client;

    public OrderApiTests()
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

    private async Task<string> CreateMenuItemAsync(string shopId)
    {
        var createMenuItemDto = new CreateMenuItemCommand(
            ObjectId.Parse(shopId),
            "Coffee",
            "Test Latte",
            "A delicious test latte",
            new MoneyDto(50000, "VND"),
            new List<string> { "Milk", "Coffee" },
            new List<string> { "Milk" },
            new AvailabilityDto(new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0), new List<int> { 0, 1, 2, 3, 4, 5, 6 })
        );

        var response = await _client.PostAsJsonAsync("/api/v1/menu", createMenuItemDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var menuItemId = await response.Content.ReadFromJsonAsync<string>();
        return menuItemId!;
    }

    private async Task<string> CreateStaffAndLoginAsync(string shopId)
    {
        // Create a staff user with Cashier role
        var createStaffDto = new CreateStaffCommand(
            "Test",
            "Cashier",
            StaffRole.Cashier,
            new ContactInfoDto("0987654321", "cashier@test.com"),
            DateTime.UtcNow,
            ObjectId.Parse(shopId)
        );

        var response = await _client.PostAsJsonAsync("/api/v1/staff", createStaffDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Now register a user account for this staff (email must match)
        var registerDto = new
        {
            email = "cashier@test.com",
            password = "Password123",
            fullName = "Test Cashier"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResultDto>();
        return authResult!.AccessToken;
    }

    private async Task<string> CreateOrderAsync(string shopId, string staffToken, string menuItemId)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(), // StaffId - we'll use a generated one since we don't have the actual staff ID
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var response = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderId = await response.Content.ReadFromJsonAsync<string>();
        return orderId!;
    }

    [Fact]
    public async Task PayAndComplete_WithValidCashPayment_CompletesOrder()
    {
        // Arrange - Create test data
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // Create an order
        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();
        orderId.Should().NotBeNullOrEmpty();

        // Update order status to Ready (3) - required for PayAndComplete
        var updateStatusDto = new UpdateOrderStatusCommand(
            ObjectId.Parse(orderId!),
            OrderStatus.Ready
        );

        var statusResponse = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/status", updateStatusDto);
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Call PayAndComplete
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            100000, // Amount matching the order total
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert
        payResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify order is completed
        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Completed);
        order.PaymentStatus.Should().Be(PaymentStatus.Paid);
        order.CompletedTime.Should().NotBeNull();
        // Verify OrderNumber is returned
        order.OrderNumber.Should().NotBeNullOrEmpty();
        order.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task GetOrders_ReturnsOrdersWithOrderNumber()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // Create an order
        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Get orders
        var getResponse = await _client.GetAsync($"/api/v1/orders?shopId={shopId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await getResponse.Content.ReadFromJsonAsync<PagedResultDto<OrderDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().NotBeEmpty();

        // Verify OrderNumber is returned for all orders
        foreach (var order in pagedResult.Items)
        {
            order.OrderNumber.Should().NotBeNullOrEmpty();
            order.OrderNumber.Should().StartWith("ORD-");
        }
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrderWithOrderNumber()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // Create an order
        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Act - Get order by ID
        var getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.OrderNumber.Should().NotBeNullOrEmpty();
        order.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task PayAndComplete_WithInsufficientAmount_ReturnsBadRequest()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Update order status to Ready
        var updateStatusDto = new UpdateOrderStatusCommand(
            ObjectId.Parse(orderId!),
            OrderStatus.Ready
        );

        await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/status", updateStatusDto);

        // Act - Try to pay with insufficient amount
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            50000, // Only half the amount
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert
        payResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PayAndComplete_WithOrderNotReady_ReturnsBadRequest()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Order is still in Placed status (1), not Ready (3)
        // Act - Try to pay and complete without setting to Ready first
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            100000,
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert - Should fail because order is not in Ready status
        payResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PayAndComplete_WithEmptyOrder_ReturnsBadRequest()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // Create an order with no items
        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>(), // Empty items
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Update order status to Ready
        var updateStatusDto = new UpdateOrderStatusCommand(
            ObjectId.Parse(orderId!),
            OrderStatus.Ready
        );

        await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}/status", updateStatusDto);

        // Act - Try to pay and complete empty order
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            0,
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert - Should fail because order has no items
        payResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PayAndComplete_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = null; // No auth

        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Act - Call PayAndComplete without auth
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            100000,
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert
        payResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PayAndComplete_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var staffToken = await CreateStaffAndLoginAsync(await CreateShopAsync());
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var nonExistentOrderId = ObjectId.GenerateNewId().ToString();

        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(nonExistentOrderId),
            100000,
            "VND"
        );

        // Act
        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{nonExistentOrderId}/pay-and-complete", payAndCompleteDto);

        // Assert
        payResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PayAndComplete_WithCancelledOrder_ReturnsBadRequest()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        var createOrderDto = new CreateOrderCommand(
            ObjectId.Parse(shopId),
            ObjectId.GenerateNewId(),
            new CustomerInfoDto("Test Customer", "0123456789", CustomerType.DineIn),
            new List<OrderItemDto>
            {
                new OrderItemDto(
                    ObjectId.Parse(menuItemId),
                    "Test Latte",
                    "A delicious test latte",
                    new MoneyDto(50000, "VND"),
                    2,
                    "",
                    new MoneyDto(100000, "VND")
                )
            },
            DateTime.UtcNow
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await createResponse.Content.ReadFromJsonAsync<string>();

        // Cancel the order
        var cancelResponse = await _client.PostAsync($"/api/v1/orders/{orderId}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - Try to pay and complete a cancelled order
        var payAndCompleteDto = new PayAndCompleteCommand(
            ObjectId.Parse(orderId!),
            100000,
            "VND"
        );

        var payResponse = await _client.PostAsJsonAsync($"/api/v1/orders/{orderId}/pay-and-complete", payAndCompleteDto);

        // Assert - Should fail because order is cancelled
        payResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}