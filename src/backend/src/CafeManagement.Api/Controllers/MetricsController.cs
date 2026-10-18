using CafeManagement.Api.Services;
using CafeManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CafeManagement.Api.Controllers;

/// <summary>
/// Controller for exposing application performance metrics and live system telemetry.
/// </summary>
[ApiController]
[Route("api/v1/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IPerformanceMetricsService _metricsService;
    private readonly IMongoDbContext _mongoDbContext;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        IPerformanceMetricsService metricsService,
        IMongoDbContext mongoDbContext,
        ILogger<MetricsController> logger)
    {
        _metricsService = metricsService;
        _mongoDbContext = mongoDbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get raw internal metric snapshots dictionary.
    /// </summary>
    [HttpGet]
    public IActionResult GetAllMetrics()
    {
        try
        {
            var metrics = _metricsService.GetMetrics();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, "Error retrieving metrics");
        }
    }

    /// <summary>
    /// Get live system telemetry and per-endpoint performance statistics.
    /// </summary>
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemDiagnostics(CancellationToken cancellationToken)
    {
        try
        {
            var proc = Process.GetCurrentProcess();
            var uptime = DateTime.UtcNow - proc.StartTime.ToUniversalTime();
            var memoryMb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
            var gcMemoryMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 1);

            // CPU Load estimation from /proc/loadavg on Linux or thread count
            double cpuUsagePercent = 0;
            try
            {
                if (System.IO.File.Exists("/proc/loadavg"))
                {
                    var text = await System.IO.File.ReadAllTextAsync("/proc/loadavg", cancellationToken);
                    var parts = text.Split(' ');
                    if (parts.Length > 0 && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var l1))
                    {
                        cpuUsagePercent = Math.Round(Math.Min(100.0, (l1 * 100.0) / Math.Max(1, Environment.ProcessorCount)), 1);
                    }
                }
            }
            catch { }

            if (cpuUsagePercent <= 0)
            {
                cpuUsagePercent = Math.Round(Math.Min(100.0, proc.Threads.Count * 1.8), 1);
            }

            // Real MongoDB ping and connectivity
            string mongoStatus = "Connected";
            double mongoPingMs = 0;
            try
            {
                var sw = Stopwatch.StartNew();
                await _mongoDbContext.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", cancellationToken: cancellationToken);
                sw.Stop();
                mongoPingMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            }
            catch (Exception ex)
            {
                mongoStatus = "Disconnected: " + ex.Message;
            }

            // Metrics from Service
            var allMetrics = _metricsService.GetMetrics();

            // Total request count and overall average latency
            var durationMetric = allMetrics.TryGetValue("http.request.duration", out var dm) ? dm : null;
            long totalRequests = durationMetric?.Count ?? 0;
            double avgResponseTimeMs = durationMetric != null ? Math.Round(durationMetric.Average, 1) : 0;
            double minResponseTimeMs = durationMetric != null ? Math.Round(durationMetric.Min, 1) : 0;
            double maxResponseTimeMs = durationMetric != null ? Math.Round(durationMetric.Max, 1) : 0;

            // Compute error count and endpoint breakdown
            long errorCount = 0;
            var endpointMap = new Dictionary<string, (string method, string path, long count, string lastStatus, double avgDurationMs)>();

            foreach (var kvp in allMetrics)
            {
                if (kvp.Key.StartsWith("http.request.count["))
                {
                    var raw = kvp.Key.Substring("http.request.count[".Length).TrimEnd(']');
                    var tags = ParseTags(raw);
                    var method = tags.GetValueOrDefault("method", "GET");
                    var path = tags.GetValueOrDefault("path", "");
                    var statusCode = tags.GetValueOrDefault("status_code", "200");

                    if (statusCode.StartsWith("5"))
                    {
                        errorCount += kvp.Value.Count;
                    }

                    var epKey = $"{method}:{path}";
                    if (!endpointMap.TryGetValue(epKey, out var val))
                    {
                        endpointMap[epKey] = (method, path, kvp.Value.Count, statusCode, 0);
                    }
                    else
                    {
                        endpointMap[epKey] = (method, path, val.count + kvp.Value.Count, statusCode, val.avgDurationMs);
                    }
                }
                else if (kvp.Key.StartsWith("http.endpoint.duration["))
                {
                    var raw = kvp.Key.Substring("http.endpoint.duration[".Length).TrimEnd(']');
                    var tags = ParseTags(raw);
                    var method = tags.GetValueOrDefault("method", "GET");
                    var path = tags.GetValueOrDefault("path", "");
                    var epKey = $"{method}:{path}";
                    if (endpointMap.TryGetValue(epKey, out var val))
                    {
                        endpointMap[epKey] = (val.method, val.path, val.count, val.lastStatus, Math.Round(kvp.Value.Average, 1));
                    }
                    else
                    {
                        endpointMap[epKey] = (method, path, kvp.Value.Count, "200", Math.Round(kvp.Value.Average, 1));
                    }
                }
            }

            double errorRatePercent = totalRequests > 0 ? Math.Round((double)errorCount / totalRequests * 100, 2) : 0.0;

            var endpoints = endpointMap.Values
                .OrderByDescending(e => e.count)
                .Select(e => new
                {
                    endpoint = e.path,
                    method = e.method,
                    count = e.count,
                    avgLatencyMs = e.avgDurationMs > 0 ? e.avgDurationMs : avgResponseTimeMs,
                    status = e.lastStatus
                })
                .Take(10)
                .ToList();

            var uptimeDays = (int)uptime.TotalDays;
            var uptimeFormatted = uptimeDays > 0
                ? $"{uptimeDays}d {uptime.Hours}h {uptime.Minutes}m"
                : $"{uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";

            return Ok(new
            {
                status = "Online",
                uptime = uptimeFormatted,
                uptimeSeconds = (long)uptime.TotalSeconds,
                avgResponseTimeMs,
                minResponseTimeMs,
                maxResponseTimeMs,
                totalRequests,
                errorRatePercent,
                cpuUsagePercent,
                memoryUsageMb = memoryMb,
                gcMemoryMb,
                threadCount = proc.Threads.Count,
                mongoDb = new
                {
                    status = mongoStatus,
                    pingMs = mongoPingMs,
                    databaseName = _mongoDbContext.Database.DatabaseNamespace.DatabaseName
                },
                endpoints,
                framework = ".NET 8.0 ASP.NET Core",
                serverTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system diagnostics");
            return StatusCode(500, new { message = "Lỗi khi lấy thông số hệ thống", error = ex.Message });
        }
    }

    private static Dictionary<string, string> ParseTags(string raw)
    {
        var dict = new Dictionary<string, string>();
        var parts = raw.Split(',');
        foreach (var p in parts)
        {
            var kv = p.Split('=');
            if (kv.Length == 2)
            {
                dict[kv[0].Trim()] = kv[1].Trim();
            }
        }
        return dict;
    }

    /// <summary>
    /// Get a specific performance metric by name and optional tags.
    /// </summary>
    [HttpGet("{name}")]
    public IActionResult GetMetric(string name, [FromQuery] Dictionary<string, string> tags)
    {
        try
        {
            var metric = _metricsService.GetMetric(name, tags.Count > 0 ? tags : null);
            if (metric == null)
            {
                return NotFound($"Metric '{name}' not found");
            }

            return Ok(metric);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metric {Name}", name);
            return StatusCode(500, $"Error retrieving metric '{name}'");
        }
    }
}