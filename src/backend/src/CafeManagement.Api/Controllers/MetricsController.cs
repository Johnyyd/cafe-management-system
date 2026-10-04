using CafeManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CafeManagement.Api.Controllers;

/// <summary>
/// Controller for exposing application performance metrics.
/// </summary>
[ApiController]
[Route("api/v1/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IPerformanceMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IPerformanceMetricsService metricsService, ILogger<MetricsController> logger)
    {
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all current performance metrics.
    /// </summary>
    /// <returns>A dictionary of metric names to their current values.</returns>
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
    /// Get a specific performance metric by name and optional tags.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="tags">Optional tags as query parameters.</param>
    /// <returns>The metric snapshot if found, null otherwise.</returns>
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