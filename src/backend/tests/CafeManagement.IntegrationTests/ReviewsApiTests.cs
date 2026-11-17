using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CafeManagement.Api;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Application.Orders.Commands;
using CafeManagement.Application.Shops.Commands;
using CafeManagement.Application.Staff.Commands;
using CafeManagement.Domain.Orders;
using CafeManagement.Domain.Staff;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Bson;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class ReviewsApiTests
{
    private readonly HttpClient _client;

    public ReviewsApiTests()
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

        var response = await _client.PostAsJsonAsync("/api/v1/orders", createOrderDto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderId = await response.Content.ReadFromJsonAsync<string>();
        return orderId!;
    }

    [Fact]
    public async Task GetOrderContext_WithObjectId_ReturnsOrderContext()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        // Act - Use ObjectId as identifier
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{orderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await response.Content.ReadFromJsonAsync<OrderContextDto>();
        context.Should().NotBeNull();
        context!.OrderId.Should().Be(orderId);
        context.ShopId.Should().Be(shopId);
        context.CustomerName.Should().Be("Test Customer");
        context.Items.Should().NotBeEmpty();
        context.OrderNumber.Should().NotBeNullOrEmpty();
        context.OrderNumber.Should().StartWith("ORD-");
    }

    [Fact]
    public async Task GetOrderContext_WithOrderNumber_ReturnsOrderContext()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        // Get the order to find its order number
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var getOrderResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await getOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        var orderNumber = order!.OrderNumber;

        // Act - Use OrderNumber as identifier (fallback logic)
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{orderNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await response.Content.ReadFromJsonAsync<OrderContextDto>();
        context.Should().NotBeNull();
        context!.OrderId.Should().Be(orderId);
        context.OrderNumber.Should().Be(orderNumber);
        context.ShopId.Should().Be(shopId);
        context.CustomerName.Should().Be("Test Customer");
    }

    [Fact]
    public async Task GetOrderContext_WithNonExistentObjectId_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = ObjectId.GenerateNewId().ToString();

        // Act
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderContext_WithInvalidOrderNumber_ReturnsNotFound()
    {
        // Arrange
        var invalidOrderNumber = "ORD-999999-ABCDEF";

        // Act
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{invalidOrderNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderContext_WithOrderNumberSuffix_MatchesCorrectOrder()
    {
        // Arrange - Create multiple orders
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        var orderId1 = await CreateOrderAsync(shopId, staffToken, menuItemId);
        var orderId2 = await CreateOrderAsync(shopId, staffToken, menuItemId);

        // Get order numbers
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var getOrder1Response = await _client.GetAsync($"/api/v1/orders/{orderId1}");
        var order1 = await getOrder1Response.Content.ReadFromJsonAsync<OrderDto>();

        var getOrder2Response = await _client.GetAsync($"/api/v1/orders/{orderId2}");
        var order2 = await getOrder2Response.Content.ReadFromJsonAsync<OrderDto>();

        // Use the suffix of the ObjectId (last 6 chars) as order number suffix
        // The fallback logic searches by suffix
        var orderId1Suffix = orderId1.Substring(orderId1.Length - 6);
        var orderId2Suffix = orderId2.Substring(orderId2.Length - 6);

        // Act - Search using ORD- + suffix (this is what the fallback does)
        var fallbackIdentifier1 = $"ORD-{orderId1Suffix}";
        var response1 = await _client.GetAsync($"/api/v1/reviews/order-context/{fallbackIdentifier1}");

        // Assert - Should find the correct order
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var context1 = await response1.Content.ReadFromJsonAsync<OrderContextDto>();
        context1.Should().NotBeNull();
        context1!.OrderId.Should().Be(orderId1);
    }

    [Fact]
    public async Task GetOrderContext_WithoutAuthentication_AllowsAnonymous()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        // Act - No authentication
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{orderId}");

        // Assert - AllowAnonymous should allow access
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var context = await response.Content.ReadFromJsonAsync<OrderContextDto>();
        context.Should().NotBeNull();
        context!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task CreateReview_Anonymous_WithValidData_ReturnsCreated()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        _client.DefaultRequestHeaders.Authorization = null; // Anonymous

        var createReviewRequest = new
        {
            shopId,
            shopName = "Test Cafe",
            orderId,
            customerName = "John Doe",
            customerPhone = "0123456789",
            shopRating = 5,
            shopComment = "Great coffee!",
            itemReviews = new[]
            {
                new { menuItemId, menuItemName = "Test Latte", rating = 5, comment = "Delicious!" }
            },
            tags = new[] { "coffee", "latte" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reviews", createReviewRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var review = await response.Content.ReadFromJsonAsync<ReviewDto>();
        review.Should().NotBeNull();
        review!.ShopId.Should().Be(shopId);
        review.CustomerName.Should().Be("John Doe");
        review.ShopRating.Should().Be(5);
        review.ItemReviews.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateReview_WithOrderId_LinksToOrder()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        _client.DefaultRequestHeaders.Authorization = null; // Anonymous

        var createReviewRequest = new
        {
            shopId,
            shopName = "Test Cafe",
            orderId,
            customerName = "Jane Smith",
            customerPhone = "0987654321",
            shopRating = 4,
            shopComment = "Good service",
            itemReviews = new object[] { },
            tags = new[] { "service" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/reviews", createReviewRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var review = await response.Content.ReadFromJsonAsync<ReviewDto>();
        review.Should().NotBeNull();
        review!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetReviews_WithShopId_ReturnsPagedReviews()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        // Create multiple orders and reviews
        for (int i = 0; i < 3; i++)
        {
            var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

            _client.DefaultRequestHeaders.Authorization = null;
            var createReviewRequest = new
            {
                shopId,
                shopName = "Test Cafe",
                orderId,
                customerName = $"Customer {i}",
                customerPhone = $"012345678{i}",
                shopRating = 5 - i,
                shopComment = $"Review {i}",
                itemReviews = new object[] { },
                tags = new string[] { }
            };
            await _client.PostAsJsonAsync("/api/v1/reviews", createReviewRequest);
        }

        // Act - Get reviews with admin token
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync($"/api/v1/reviews?shopId={shopId}&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ReviewDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCountGreaterOrEqualTo(3);
        result.TotalCount.Should().BeGreaterOrEqualTo(3);
    }

    private async Task<string> CreateAdminAndLoginAsync(string shopId, string email = "admin@test.com")
    {
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

    [Fact]
    public async Task GetReviewStats_WithShopId_ReturnsCorrectStats()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);

        // Create orders and reviews with different ratings
        for (int i = 0; i < 5; i++)
        {
            var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

            _client.DefaultRequestHeaders.Authorization = null;
            var createReviewRequest = new
            {
                shopId,
                shopName = "Test Cafe",
                orderId,
                customerName = $"Customer {i}",
                customerPhone = $"012345678{i}",
                shopRating = 5 - (i % 5), // Ratings: 5, 4, 3, 2, 1
                shopComment = $"Review {i}",
                itemReviews = new[]
                {
                    new { menuItemId, menuItemName = "Test Latte", rating = 5 - (i % 5), comment = "Good" }
                },
                tags = new[] { "coffee", "latte" }
            };
            await _client.PostAsJsonAsync("/api/v1/reviews", createReviewRequest);
        }

        // Act - Get stats with admin token
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync($"/api/v1/reviews/stats?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<ReviewStatsDto>();
        stats.Should().NotBeNull();
        stats!.TotalReviews.Should().Be(5);
        stats.AverageShopRating.Should().Be(3.0); // (5+4+3+2+1)/5 = 3.0
        stats.SatisfactionRate.Should().Be(40.0); // 2 out of 5 >= 4 (ratings 5 and 4)

        stats.RatingDistribution.Should().NotBeNull();
        stats.RatingDistribution["5"].Should().Be(1);
        stats.RatingDistribution["4"].Should().Be(1);
        stats.RatingDistribution["3"].Should().Be(1);
        stats.RatingDistribution["2"].Should().Be(1);
        stats.RatingDistribution["1"].Should().Be(1);

        stats.TopProducts.Should().NotBeNull();
        stats.TopProducts.Should().HaveCount(1);
        stats.TopProducts[0].Count.Should().Be(5);

        stats.TopTags.Should().NotBeNull();
        stats.TopTags.Should().HaveCount(2); // "coffee" and "latte"
    }

    [Fact]
    public async Task GetReviewStats_WithoutReviews_ReturnsZeroStats()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var adminToken = await CreateAdminAndLoginAsync(shopId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.GetAsync($"/api/v1/reviews/stats?shopId={shopId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<ReviewStatsDto>();
        stats.Should().NotBeNull();
        stats!.TotalReviews.Should().Be(0);
        stats.AverageShopRating.Should().Be(0.0);
        stats.SatisfactionRate.Should().Be(0.0);
        stats.RatingDistribution.Should().NotBeNull();
        stats.TopProducts.Should().NotBeNull().And.BeEmpty();
        stats.TopTags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetReviewStats_WithoutShopId_ReturnsStatsForAllShops()
    {
        // Arrange
        var shopId1 = await CreateShopAsync("Shop 1");
        var shopId2 = await CreateShopAsync("Shop 2");
        var menuItemId1 = await CreateMenuItemAsync(shopId1);
        var menuItemId2 = await CreateMenuItemAsync(shopId2);
        var staffToken = await CreateStaffAndLoginAsync(shopId1);

        // Create reviews in both shops
        for (int i = 0; i < 2; i++)
        {
            var orderId1 = await CreateOrderAsync(shopId1, staffToken, menuItemId1);
            var orderId2 = await CreateOrderAsync(shopId2, staffToken, menuItemId2);

            _client.DefaultRequestHeaders.Authorization = null;
            await _client.PostAsJsonAsync("/api/v1/reviews", new
            {
                shopId = shopId1,
                shopName = "Shop 1",
                orderId = orderId1,
                customerName = $"Customer {i}A",
                customerPhone = $"012345678{i}",
                shopRating = 5,
                shopComment = "Great",
                itemReviews = new object[] { },
                tags = new string[] { }
            });
            await _client.PostAsJsonAsync("/api/v1/reviews", new
            {
                shopId = shopId2,
                shopName = "Shop 2",
                orderId = orderId2,
                customerName = $"Customer {i}B",
                customerPhone = $"098765432{i}",
                shopRating = 4,
                shopComment = "Good",
                itemReviews = new object[] { },
                tags = new string[] { }
            });
        }

        var adminToken = await CreateAdminAndLoginAsync(shopId1);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act - No shopId filter
        var response = await _client.GetAsync("/api/v1/reviews/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stats = await response.Content.ReadFromJsonAsync<ReviewStatsDto>();
        stats.Should().NotBeNull();
        stats!.TotalReviews.Should().Be(4);
    }

    [Fact]
    public async Task GetOrderContext_WithInvalidObjectIdFormat_ReturnsNotFound()
    {
        // Arrange
        var invalidIdentifier = "not-a-valid-id";

        // Act
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{invalidIdentifier}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderContext_WithOrderNumberPrefixButNoMatch_ReturnsNotFound()
    {
        // Arrange - Use a valid ORD- format but no matching order
        var nonExistentOrderNumber = "ORD-240115-ABCDEF";

        // Act
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{nonExistentOrderNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderContext_OrderNumberExactMatch_Works()
    {
        // Arrange
        var shopId = await CreateShopAsync();
        var menuItemId = await CreateMenuItemAsync(shopId);
        var staffToken = await CreateStaffAndLoginAsync(shopId);
        var orderId = await CreateOrderAsync(shopId, staffToken, menuItemId);

        // Get the order to find its order number
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);
        var getOrderResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await getOrderResponse.Content.ReadFromJsonAsync<OrderDto>();
        var orderNumber = order!.OrderNumber;

        // Act - Use exact OrderNumber
        var response = await _client.GetAsync($"/api/v1/reviews/order-context/{orderNumber}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var context = await response.Content.ReadFromJsonAsync<OrderContextDto>();
        context.Should().NotBeNull();
        context!.OrderId.Should().Be(orderId);
        context.OrderNumber.Should().Be(orderNumber);
    }
}

// DTOs for deserialization
public class OrderContextDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<ReviewItemDto> Items { get; set; } = new();
}

public class ReviewItemDto
{
    public string MenuItemId { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class ReviewDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int ShopRating { get; set; }
    public string ShopComment { get; set; } = string.Empty;
    public List<ReviewItemDto> ItemReviews { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ReviewStatsDto
{
    public int TotalReviews { get; set; }
    public double AverageShopRating { get; set; }
    public double SatisfactionRate { get; set; }
    public Dictionary<string, int> RatingDistribution { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopTagDto> TopTags { get; set; } = new();
}

public class TopProductDto
{
    public string MenuItemId { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageRating { get; set; }
}

public class TopTagDto
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
}