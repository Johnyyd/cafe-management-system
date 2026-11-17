using CafeManagement.Api.Authorization;
using CafeManagement.Application.Common.Dtos;
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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Api.Controllers;

internal static class ReviewSanitizer
{
    private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly int MaxCommentLength = 1000;
    private static readonly int MaxTagLength = 50;

    public static string SanitizeComment(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var stripped = HtmlTagRegex.Replace(input, string.Empty);
        return stripped.Length > MaxCommentLength ? stripped[..MaxCommentLength] : stripped;
    }

    public static string SanitizeTag(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var stripped = HtmlTagRegex.Replace(input, string.Empty);
        return stripped.Length > MaxTagLength ? stripped[..MaxTagLength] : stripped;
    }

    public static List<string> SanitizeTags(List<string>? tags)
    {
        if (tags == null || tags.Count == 0) return new List<string>();
        return tags.Select(SanitizeTag).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
    }
}

// Internal MongoDB document - not exposed via API
internal class ReviewDocument
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
    public List<ReviewItemDocument> ItemReviews { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

internal class ReviewItemDocument
{
    public string MenuItemId { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string? Comment { get; set; }
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

    private IMongoCollection<ReviewDocument> GetCollection()
    {
        return _mongoDbContext.GetCollection<ReviewDocument>("reviews");
    }

    private static ReviewDto MapToDto(ReviewDocument doc)
    {
        return new ReviewDto(
            Id: ObjectId.Parse(doc.Id),
            ShopId: ObjectId.Parse(doc.ShopId),
            ShopName: doc.ShopName,
            OrderId: doc.OrderId != null ? ObjectId.Parse(doc.OrderId) : null,
            CustomerName: doc.CustomerName,
            CustomerPhone: doc.CustomerPhone,
            ShopRating: doc.ShopRating,
            ShopComment: doc.ShopComment,
            ItemReviews: doc.ItemReviews.Select(i => new ReviewItemDto(
                ObjectId.Parse(i.MenuItemId),
                i.MenuItemName,
                i.Rating,
                i.Comment
            )).ToList(),
            Tags: doc.Tags,
            CreatedAt: doc.CreatedAt
        );
    }

    [HttpGet]
    [Authorize(Policy = "ShopScope")]
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
            var builder = Builders<ReviewDocument>.Filter;
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
                var s = Regex.Escape(search.Trim());
                var searchFilter = builder.Regex(r => r.CustomerName, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.CustomerPhone, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.ShopComment, new BsonRegularExpression(s, "i"))
                                 | builder.Regex(r => r.ShopName, new BsonRegularExpression(s, "i"));
                filter &= searchFilter;
            }

            var totalCount = await col.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            var safePage = Math.Max(1, page);
            var safePageSize = Math.Clamp(pageSize, 1, 200);
            var items = await col.Find(filter)
                .SortByDescending(r => r.CreatedAt)
                .Skip((safePage - 1) * safePageSize)
                .Limit(safePageSize)
                .ToListAsync(cancellationToken);

            var totalPages = (int)Math.Ceiling((double)totalCount / safePageSize);

            return Ok(new
            {
                items = items.Select(MapToDto),
                totalCount,
                page = safePage,
                pageSize = safePageSize,
                totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reviews");
            return StatusCode(500, new { message = "Lỗi khi tải danh sách đánh giá" });
        }
    }

    [HttpGet("stats")]
    [Authorize(Policy = "ShopScope")]
    public async Task<IActionResult> GetReviewStats(
        [FromQuery] string? shopId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var col = GetCollection();
            var builder = Builders<ReviewDocument>.Filter;
            var filter = builder.Empty;

            if (!string.IsNullOrWhiteSpace(shopId))
            {
                filter &= builder.Eq(r => r.ShopId, shopId);
            }

            // Use aggregation pipeline for server-side computation
            var pipeline = new List<BsonDocument>
            {
                new("$match", filter.Render(col.DocumentSerializer, col.Settings.SerializerRegistry)),
                new("$facet", new BsonDocument
                {
                    { "summary", new BsonArray
                        {
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", BsonNull.Value },
                                { "totalReviews", new BsonDocument("$sum", 1) },
                                { "averageShopRating", new BsonDocument("$avg", "$ShopRating") },
                                { "satisfiedCount", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$gte", new BsonArray { "$ShopRating", 4 }),
                                        1,
                                        0
                                    })) },
                                { "rating5", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$ShopRating", 5 }),
                                        1,
                                        0
                                    })) },
                                { "rating4", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$ShopRating", 4 }),
                                        1,
                                        0
                                    })) },
                                { "rating3", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$ShopRating", 3 }),
                                        1,
                                        0
                                    })) },
                                { "rating2", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$ShopRating", 2 }),
                                        1,
                                        0
                                    })) },
                                { "rating1", new BsonDocument("$sum",
                                    new BsonDocument("$cond", new BsonArray
                                    {
                                        new BsonDocument("$eq", new BsonArray { "$ShopRating", 1 }),
                                        1,
                                        0
                                    })) }
                            })
                        }
                    },
                    { "topProducts", new BsonArray
                        {
                            new BsonDocument("$unwind", "$ItemReviews"),
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", new BsonDocument
                                    {
                                        { "menuItemId", "$ItemReviews.MenuItemId" },
                                        { "menuItemName", "$ItemReviews.MenuItemName" }
                                    }
                                },
                                { "count", new BsonDocument("$sum", 1) },
                                { "averageRating", new BsonDocument("$avg", "$ItemReviews.Rating") }
                            }),
                            new BsonDocument("$sort", new BsonDocument { { "count", -1 }, { "averageRating", -1 } }),
                            new BsonDocument("$limit", 6),
                            new BsonDocument("$project", new BsonDocument
                            {
                                { "menuItemId", "$_id.menuItemId" },
                                { "menuItemName", "$_id.menuItemName" },
                                { "count", 1 },
                                { "averageRating", new BsonDocument("$round", new BsonArray { "$averageRating", 1 }) }
                            })
                        }
                    },
                    { "topTags", new BsonArray
                        {
                            new BsonDocument("$unwind", "$Tags"),
                            new BsonDocument("$group", new BsonDocument
                            {
                                { "_id", "$Tags" },
                                { "count", new BsonDocument("$sum", 1) }
                            }),
                            new BsonDocument("$sort", new BsonDocument { { "count", -1 } }),
                            new BsonDocument("$limit", 8),
                            new BsonDocument("$project", new BsonDocument
                            {
                                { "tag", "$_id" },
                                { "count", 1 }
                            })
                        }
                    }
                })
            };

            var result = await col.Aggregate<BsonDocument>(pipeline).FirstOrDefaultAsync(cancellationToken);

            if (result == null || !result.Contains("summary") || result["summary"].AsBsonArray.Count == 0)
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

            var summary = result["summary"].AsBsonArray[0].AsBsonDocument;
            var totalReviews = summary["totalReviews"].AsInt32;
            var avgRating = summary.Contains("averageShopRating") && !summary["averageShopRating"].IsBsonNull
                ? Math.Round(summary["averageShopRating"].AsDouble, 1)
                : 0.0;
            var satisfiedCount = summary["satisfiedCount"].AsInt32;
            var satisfactionRate = totalReviews > 0 ? Math.Round((double)satisfiedCount / totalReviews * 100, 1) : 0.0;

            var dist = new Dictionary<string, int>
            {
                { "5", summary["rating5"].AsInt32 },
                { "4", summary["rating4"].AsInt32 },
                { "3", summary["rating3"].AsInt32 },
                { "2", summary["rating2"].AsInt32 },
                { "1", summary["rating1"].AsInt32 }
            };

            var topProducts = new List<object>();
            if (result.Contains("topProducts") && result["topProducts"].IsBsonArray)
            {
                foreach (var p in result["topProducts"].AsBsonArray)
                {
                    topProducts.Add(new
                    {
                        menuItemId = p["menuItemId"].AsString,
                        menuItemName = p["menuItemName"].AsString,
                        count = p["count"].AsInt32,
                        averageRating = p["averageRating"].AsDouble
                    });
                }
            }

            var topTags = new List<object>();
            if (result.Contains("topTags") && result["topTags"].IsBsonArray)
            {
                foreach (var t in result["topTags"].AsBsonArray)
                {
                    topTags.Add(new
                    {
                        tag = t["tag"].AsString,
                        count = t["count"].AsInt32
                    });
                }
            }

            return Ok(new
            {
                totalReviews,
                averageShopRating = avgRating,
                satisfactionRate,
                ratingDistribution = dist,
                topProducts,
                topTags
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review stats");
            return StatusCode(500, new { message = "Lỗi khi lấy thống kê đánh giá" });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { message = "Vui lòng nhập họ và tên khách hàng." });

        if (request.ShopId == ObjectId.Empty)
            return BadRequest(new { message = "Vui lòng chọn chi nhánh cửa hàng." });

        if (request.ShopRating < 1 || request.ShopRating > 5)
            return BadRequest(new { message = "Điểm đánh giá phải từ 1 đến 5 sao." });

        // Optional: verify OrderId exists if provided (prevents spam)
        if (request.OrderId.HasValue && request.OrderId.Value != ObjectId.Empty)
        {
            var ordersCol = _mongoDbContext.GetCollection<BsonDocument>("orders");
            var orderExists = await ordersCol.Find(Builders<BsonDocument>.Filter.Eq("_id", request.OrderId.Value))
                .AnyAsync(cancellationToken);
            if (!orderExists)
                return BadRequest(new { message = "Đơn hàng không tồn tại." });
        }

        try
        {
            var col = GetCollection();
            var doc = new ReviewDocument
            {
                ShopId = request.ShopId.ToString(),
                ShopName = request.ShopName,
                OrderId = request.OrderId?.ToString(),
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone?.Trim() ?? string.Empty,
                ShopRating = request.ShopRating,
                ShopComment = ReviewSanitizer.SanitizeComment(request.ShopComment),
                ItemReviews = request.ItemReviews?.Select(i => new ReviewItemDocument
                {
                    MenuItemId = i.MenuItemId.ToString(),
                    MenuItemName = i.MenuItemName,
                    Rating = i.Rating,
                    Comment = ReviewSanitizer.SanitizeComment(i.Comment)
                }).ToList() ?? new List<ReviewItemDocument>(),
                Tags = ReviewSanitizer.SanitizeTags(request.Tags?.ToList()),
                CreatedAt = request.CreatedAt ?? DateTime.UtcNow
            };

            await col.InsertOneAsync(doc, cancellationToken: cancellationToken);
            return StatusCode(StatusCodes.Status201Created, MapToDto(doc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return StatusCode(500, new { message = "Lỗi khi lưu đánh giá" });
        }
    }

    [HttpGet("order-context/{identifier}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderContext(string identifier, CancellationToken cancellationToken = default)
    {
        try
        {
            var ordersCol = _mongoDbContext.GetCollection<BsonDocument>("orders");
            var shopsCol = _mongoDbContext.GetCollection<BsonDocument>("shops");

            BsonDocument? orderDoc = null;

            if (ObjectId.TryParse(identifier, out var objId))
            {
                orderDoc = await ordersCol.Find(Builders<BsonDocument>.Filter.Eq("_id", objId))
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Fallback: query by OrderNumber field directly (indexed)
            if (orderDoc == null && identifier.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
            {
                orderDoc = await ordersCol.Find(Builders<BsonDocument>.Filter.Eq("OrderNumber", identifier))
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (orderDoc == null)
            {
                return NotFound(new { message = "Không tìm thấy đơn hàng tương ứng." });
            }

            var shopId = orderDoc.Contains("ShopId") ? orderDoc["ShopId"].ToString() : string.Empty;
            string shopName = "The Coffee House";

            if (!string.IsNullOrEmpty(shopId) && ObjectId.TryParse(shopId, out var shopObjId))
            {
                var shopDoc = await shopsCol.Find(Builders<BsonDocument>.Filter.Eq("_id", shopObjId))
                    .FirstOrDefaultAsync(cancellationToken);
                if (shopDoc != null && shopDoc.Contains("Name"))
                {
                    shopName = shopDoc["Name"].AsString;
                }
            }

            var customerInfo = orderDoc.Contains("CustomerInfo") && orderDoc["CustomerInfo"].IsBsonDocument
                ? orderDoc["CustomerInfo"].AsBsonDocument
                : null;
            var customerName = customerInfo != null && customerInfo.Contains("Name")
                ? customerInfo["Name"].AsString
                : string.Empty;

            var itemsList = new List<object>();
            if (orderDoc.Contains("Items") && orderDoc["Items"].IsBsonArray)
            {
                foreach (var it in orderDoc["Items"].AsBsonArray)
                {
                    if (it.IsBsonDocument)
                    {
                        var doc = it.AsBsonDocument;
                        var mId = doc.Contains("MenuItemId") ? doc["MenuItemId"].ToString() : "";
                        var mName = doc.Contains("Name") ? doc["Name"].AsString : "";
                        var qty = doc.Contains("Quantity") ? doc["Quantity"].ToInt32() : 1;
                        itemsList.Add(new { menuItemId = mId, menuItemName = mName, quantity = qty });
                    }
                }
            }

            var orderNumber = orderDoc.Contains("OrderNumber") && !string.IsNullOrEmpty(orderDoc["OrderNumber"].AsString)
                ? orderDoc["OrderNumber"].AsString
                : $"ORD-{orderDoc["_id"].AsObjectId.ToString().Substring(18).ToUpperInvariant()}";

            return Ok(new
            {
                orderId = orderDoc["_id"].ToString(),
                orderNumber,
                shopId,
                shopName,
                customerName,
                items = itemsList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order context for {Identifier}", identifier);
            return StatusCode(500, new { message = "Lỗi khi tra cứu thông tin đơn hàng" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteReview(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var col = GetCollection();
            if (!ObjectId.TryParse(id, out var objId))
                return BadRequest(new { message = "ID không hợp lệ" });

            var res = await col.DeleteOneAsync(r => r.Id == id, cancellationToken: cancellationToken);
            if (res.DeletedCount == 0)
                return NotFound(new { message = "Không tìm thấy đánh giá" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {Id}", id);
            return StatusCode(500, new { message = "Lỗi khi xóa đánh giá" });
        }
    }
}
