using System.Net;
using System.Net.Http.Json;
using CafeManagement.Api;
using CafeManagement.Application.Common.Dtos;
using CafeManagement.Domain.Shops;
using FluentAssertions;
using MongoDB.Bson;
using Testcontainers.MongoDb;
using Xunit;

namespace CafeManagement.IntegrationTests;

public class ShopApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly MongoDbContainer _mongoDbContainer;

    public ShopApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _mongoDbContainer = factory.MongoDbContainer;
    }

    [Fact]
    public async Task CreateShop_WithValidData_ReturnsCreatedShop()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Test Cafe",
            new AddressDto("123 Test Street", "Hanoi", "Ba Dinh", "100000"),
            new ContactInfoDto("0123456789", "test@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0)), // Sunday
                new OperatingHoursDto(1, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))  // Monday
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdShop = await response.Content.ReadFromJsonAsync<ObjectIdDto>();
        createdShop.Should().NotBeNull();
        createdShop!.Id.Should().NotBeEmpty();

        // Verify the shop was actually created by fetching it
        var getResponse = await _client.GetAsync($"/api/v1/shops/{createdShop.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedShop = await getResponse.Content.ReadFromJsonAsync<ShopDto>();
        retrievedShop.Should().NotBeNull();
        retrievedShop!.Name.Should().Be(createShopDto.Name);
        retrievedShop.Address.Should.BeEquivalentTo(createShopDto.Address);
        retrievedShop.Contact.Should.BeEquivalentTo(createShopDto.Contact);
    }

    [Fact]
    public async Task CreateShop_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Duplicate Cafe",
            new AddressDto("123 Test Street", "Hanoi", "Ba Dinh", "100000"),
            new ContactInfoDto("0123456789", "test@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0))
            }
        );

        // Create first shop
        var firstResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Try to create another shop with same name
        var secondResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problemDetails = await secondResponse.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task GetShopById_WithValidId_ReturnsShop()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Get Test Cafe",
            new AddressDto("456 Get Street", "Hanoi", "Hai Ba Trung", "100000"),
            new ContactInfoDto("0987654321", "get@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(7, 0, 0), new TimeSpan(21, 0, 0))
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdShop = await createResponse.Content.ReadFromJsonAsync<ObjectIdDto>();

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/shops/{createdShop!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedShop = await getResponse.Content.ReadFromJsonAsync<ShopDto>();
        retrievedShop.Should().NotBeNull();
        retrievedShop!.Name.Should().Be(createShopDto.Name);
        retrievedShop.Id.Should().Be(createdShop.Id);
    }

    [Fact]
    public async Task GetShopById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = ObjectId.GenerateNewId().ToString();

        // Act
        var response = await _client.GetAsync($"/api/v1/shops/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShops_ReturnsPagedListOfShops()
    {
        // Arrange
        var shop1Dto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Shop 1",
            new AddressDto("111 Shop Street", "Hanoi", "Hoan Kiem", "100000"),
            new ContactInfoDto("0111111111", "shop1@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(8, 0, 0), new TimeSpan(20, 0, 0))
            }
        );

        var shop2Dto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Shop 2",
            new AddressDto("222 Shop Street", "Hanoi", "Hoan Kiem", "100000"),
            new ContactInfoDto("0222222222", "shop2@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(9, 0, 0), new TimeSpan(19, 0, 0))
            }
        );

        // Create two shops
        var shop1Response = await _client.PostAsJsonAsync("/api/v1/shops", shop1Dto);
        shop1Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var shop2Response = await _client.PostAsJsonAsync("/api/v1/shops", shop2Dto);
        shop2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync("/api/v1/shops");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResultDto<ShopDto>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCountGreaterOrEqualTo(2);
        pagedResult.Items.Should().Contain(s => s.Name == "Shop 1");
        pagedResult.Items.Should().Contain(s => s.Name == "Shop 2");
    }

    [Fact]
    public async Task UpdateShop_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Update Test Cafe",
            new AddressDto("789 Update Street", "Hanoi", "Dong Da", "100000"),
            new ContactInfoDto("0111222333", "update@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(7, 0, 0), new TimeSpan(20, 0, 0))
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdShop = await createResponse.Content.ReadFromJsonAsync<ObjectIdDto>();

        var updateShopDto = new CafeManagement.Application.Shops.Commands.UpdateShopCommand(
            ObjectId.Parse(createdShop!.Id),
            "Updated Cafe Name",
            new AddressDto("999 Updated Street", "Hanoi", "Dong Da", "100000"),
            new ContactInfoDto("0333444555", "updated@example.com")
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/shops/{createdShop.Id}", updateShopDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/v1/shops/{createdShop.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedShop = await getResponse.Content.ReadFromJsonAsync<ShopDto>();
        updatedShop.Should().NotBeNull();
        updatedShop!.Name.Should().Be(updateShopDto.Name);
        updatedShop.Address.Should.BeEquivalentTo(updateShopDto.Address);
        updatedShop.Contact.Should.BeEquivalentTo(updateShopDto.Contact);
    }

    [Fact]
    public async Task DeactivateShop_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Deactivate Test Cafe",
            new AddressDto("321 Deactivate Street", "Hanoi", "Tay Ho", "100000"),
            new ContactInfoDto("0444555666", "deactivate@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(8, 0, 0), new TimeSpan(18, 0, 0))
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdShop = await createResponse.Content.ReadFromJsonAsync<ObjectIdDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/shops/{createdShop!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the shop is deactivated (soft deleted)
        var getResponse = await _client.GetAsync($"/api/v1/shops/{createdShop.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOperatingHours_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var createShopDto = new CafeManagement.Application.Shops.Commands.CreateShopCommand(
            "Hours Test Cafe",
            new AddressDto("654 Hours Street", "Hanoi", "Hoang Mai", "100000"),
            new ContactInfoDto("0555666777", "hours@example.com"),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0)) // Sunday
            }
        );

        var createResponse = await _client.PostAsJsonAsync("/api/v1/shops", createShopDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdShop = await createResponse.Content.ReadFromJsonAsync<ObjectIdDto>();

        var updateHoursDto = new CafeManagement.Application.Shops.Commands.UpdateOperatingHoursCommand(
            ObjectId.Parse(createdShop!.Id),
            new List<OperatingHoursDto>
            {
                new OperatingHoursDto(0, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0)), // Sunday
                new OperatingHoursDto(1, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))  // Monday
            }
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/shops/{createdShop.Id}/hours", updateHoursDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the operating hours were updated
        var getResponse = await _client.GetAsync($"/api/v1/shops/{createdShop.Id}/hours");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var hours = await getResponse.Content.ReadFromJsonAsync<List<OperatingHoursDto>>();
        hours.Should().NotBeNull();
        hours!.Should().HaveCount(2);
        hours.Should().Contain(h => h.DayOfWeek == 0 && h.OpenTime == new TimeSpan(9, 0, 0) && h.CloseTime == new TimeSpan(18, 0, 0));
        hours.Should().Contain(h => h.DayOfWeek == 1 && h.OpenTime == new TimeSpan(9, 0, 0) && h.CloseTime == new TimeSpan(18, 0, 0));
    }
}

// DTOs for API responses that match the actual API contracts
public class ObjectIdDto
{
    public string Id { get; set; } = default!;
}

public class ValidationProblemDetailsDto
{
    public Dictionary<string, string[]> Errors { get; set; } = default!;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = default!;
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page < ((int)Math.Ceiling((double)TotalCount / PageSize));
    public bool HasPreviousPage => Page > 1;
}