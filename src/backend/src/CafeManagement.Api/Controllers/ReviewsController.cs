using CafeManagement.Infrastructure.Persistence;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Api.Controllers;

public class ReviewItemDto
{
    public string MenuItemId { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
}

public class CreateReviewRequest
{
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int ShopRating { get; set; } = 5;
    public string ShopComment { get; set; } = string.Empty;
    public List<ReviewItemDto> ItemReviews { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime? CreatedAt { get; set; }
}

public class CustomerReviewDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int ShopRating { get; set; } = 5;
    public string ShopComment { get; set; } = string.Empty;
    public List<ReviewItemDto> ItemReviews { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IMongoDbContext _mongoDbContext;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IMongoDbContext mongoDbContext, ILogger<ReviewsController> logger)
    {
        _mongoDbContext = mongoDbContext;
        _logger = logger;
    }

    private IMongoCollection<CustomerReviewDocument> GetCollection()
    {
        return _mongoDbContext.GetCollection<CustomerReviewDocument>("reviews");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        [FromQuery] string? shopId,
        [FromQuery] int? rating,
        [FromQuery] string? menuItemId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var col = GetCollection();
            var builder = Builders<CustomerReviewDocument>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(shopId))
            {
                filter &= builder.Eq(r => r.ShopId, shopId);
            }

            if (rating.HasValue && rating.Value > 0)
            {
                filter &= builder.Eq(r => r.ShopRating, rating.Value);
            }

            if (!string.IsNullOrWhiteSpace(menuItemId))
            {
                filter &= builder.ElemMatch(r => r.ItemReviews, it => it.MenuItemId == menuItemId);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var searchFilter = builder.Regex(r => r.CustomerName, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.CustomerPhone, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.ShopComment, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.ShopName, new BsonRegularExpression(s, "i"));
                filter &= searchFilter;
            }

            var totalCount = await col.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var items = await col.Find(filter)
                .SortByDescending(r => r.CreatedAt)
                .Skip((Math.Max(1, page) - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews");
            return StatusCode(500, new { message = "Lỗi khi tải danh sách đánh giá", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewStats(
        [FromQuery] string? shopId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var col = GetCollection();
            var builder = Builders<CustomerReviewDocument>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(shopId))
            {
                filter &= builder.Eq(r => r.ShopId, shopId);
            }

            var all = await col.Find(filter).ToListAsync(cancellationToken);

            if (all.Count == 0)
            {
                return Ok(new
                {
                    totalReviews = 0,
                    averageShopRating = 0.0,
                    satisfactionRate = 0.0,
                    ratingDistribution = new Dictionary<string, int>
                    {
                        { "5", 0 }, { "4", 0 }, { "3", 0 }, { "2", 0 }, { "1", 0 }
                    },
                    topProducts = new List<object>(),
                    topTags = new List<object>()
                });
            }

            var totalReviews = all.Count;
            var avgRating = Math.Round(all.Average(r => r.ShopRating), 1);
            var satisfiedCount = all.Count(r => r.ShopRating >= 4);
            var satisfactionRate = Math.Round((double)satisfiedCount / totalReviews * 100, 1);

            var dist = new Dictionary<string, int>
            {
                { "5", all.Count(r => r.ShopRating == 5) },
                { "4", all.Count(r => r.ShopRating == 4) },
                { "3", all.Count(r => r.ShopRating == 3) },
                { "2", all.Count(r => r.ShopRating == 2) },
                { "1", all.Count(r => r.ShopRating == 1) },
            };

            // Aggregate item reviews
            var productRatings = all
                .SelectMany(r => r.ItemReviews)
                .GroupBy(it => new { it.MenuItemId, it.MenuItemName })
                .Select(g => new
                {
                    menuItemId = g.Key.MenuItemId,
                    menuItemName = g.Key.MenuItemName,
                    count = g.Count(),
                    averageRating = Math.Round(g.Average(x => x.Rating), 1)
                })
                .OrderByDescending(x => x.count)
                .ThenByDescending(x => x.averageRating)
                .Take(6)
                .ToList();

            // Aggregate tags
            var tagStats = all
                .SelectMany(r => r.Tags ?? new List<string>())
                .GroupBy(t => t)
                .Select(g => new { tag = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(8)
                .ToList();

            return Ok(new
            {
                totalReviews,
                averageShopRating = avgRating,
                satisfactionRate,
                ratingDistribution = dist,
                topProducts = productRatings,
                topTags = tagStats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review stats");
            return StatusCode(500, new { message = "Lỗi khi lấy thống kê đánh giá", error = ex.Message });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên khách hàng." });

        if (string.IsNullOrWhiteSpace(request.ShopId))
            return BadRequest(new { message = "Vui lòng chọn chi nhánh cửa hàng." });

        if (request.ShopRating < 1 || request.ShopRating > 5)
            return BadRequest(new { message = "Điểm đánh giá phải từ 1 đến 5 sao." });

        try
        {
            var col = GetCollection();
            var doc = new CustomerReviewDocument
            {
                ShopId = request.ShopId,
                ShopName = request.ShopName,
                OrderId = request.OrderId,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone?.Trim() ?? string.Empty,
                ShopRating = request.ShopRating,
                ShopComment = request.ShopComment?.Trim() ?? string.Empty,
                ItemReviews = request.ItemReviews ?? new List<ReviewItemDto>(),
                Tags = request.Tags ?? new List<string>(),
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow
            };

            await col.InsertOneAsync(doc, cancellationToken: cancellationToken);
            return StatusCode(StatusCodes.Status201Created, doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { message = "Lỗi khi lưu đánh giá", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteReview(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var col = GetCollection();
            var res = await col.DeleteOneAsync(r => r.Id == id, cancellationToken: cancellationToken);
            if (res.DeletedCount == 0)
                return NotFound(new { message = "Không tìm thấy đánh giá" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {Id}", id);
            return StatusCode(500, new { message = "Lỗi khi xóa đánh giá", error = ex.Message });
        }
    }
}
