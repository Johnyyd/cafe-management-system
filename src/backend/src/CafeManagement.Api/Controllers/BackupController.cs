using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Api.Controllers;

[ApiController]
[Route("api/v1/system/backup")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly IMongoDbContext _mongoDbContext;
    private readonly ILogger<BackupController> _logger;

    private static readonly string[] Collections = new[]
    {
        "shops",
        "menuItems",
        "inventoryItems",
        "orders",
        "staff",
        "users"
    };

    public BackupController(IMongoDbContext mongoDbContext, ILogger<BackupController> logger)
    {
        _mongoDbContext = mongoDbContext;
        _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var counts = new Dictionary<string, long>();
        foreach (var col in Collections)
        {
            var collection = _mongoDbContext.GetCollection<BsonDocument>(col);
            var count = await collection.CountDocumentsAsync(new BsonDocument(), cancellationToken: cancellationToken);
            counts[col] = count;
        }

        return Ok(new
        {
            DatabaseName = _mongoDbContext.Database.DatabaseNamespace.DatabaseName,
            TotalCollections = Collections.Length,
            Collections = counts,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportBackup(CancellationToken cancellationToken)
    {
        var backupData = new Dictionary<string, List<object>>();
        long totalDocs = 0;

        foreach (var col in Collections)
        {
            var collection = _mongoDbContext.GetCollection<BsonDocument>(col);
            var docs = await collection.Find(new BsonDocument()).ToListAsync(cancellationToken);
            totalDocs += docs.Count;
            backupData[col] = docs.Select(d => JsonSerializer.Deserialize<object>(d.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson }))!).ToList();
        }

        var result = new
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            TotalDocuments = totalDocs,
            Data = backupData
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"cafe_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

        return File(bytes, "application/json", fileName);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreBackup([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        try
        {
            if (!payload.TryGetProperty("data", out var dataProp) && !payload.TryGetProperty("Data", out dataProp))
            {
                return BadRequest(new { message = "Dữ liệu backup không hợp lệ: thiếu trường 'data'." });
            }

            var restoredCounts = new Dictionary<string, int>();

            foreach (var col in Collections)
            {
                JsonElement colElement;
                if (!dataProp.TryGetProperty(col, out colElement) && 
                    !dataProp.TryGetProperty(char.ToUpper(col[0]) + col[1..], out colElement))
                {
                    continue;
                }

                if (colElement.ValueKind != JsonValueKind.Array)
                    continue;

                var docsToInsert = new List<BsonDocument>();
                foreach (var item in colElement.EnumerateArray())
                {
                    var rawJson = item.GetRawText();
                    docsToInsert.Add(BsonDocument.Parse(rawJson));
                }

                var collection = _mongoDbContext.GetCollection<BsonDocument>(col);
                await collection.DeleteManyAsync(new BsonDocument(), cancellationToken);

                if (docsToInsert.Count > 0)
                {
                    await collection.InsertManyAsync(docsToInsert, cancellationToken: cancellationToken);
                }

                restoredCounts[col] = docsToInsert.Count;
            }

            _logger.LogInformation("Database restored successfully from backup.");
            return Ok(new
            {
                Success = true,
                Message = "Khôi phục dữ liệu hệ thống thành công!",
                RestoredAt = DateTime.UtcNow,
                RestoredCounts = restoredCounts
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi khôi phục dữ liệu từ backup.");
            return BadRequest(new { message = "Lỗi khi khôi phục dữ liệu: " + ex.Message });
        }
    }
}
