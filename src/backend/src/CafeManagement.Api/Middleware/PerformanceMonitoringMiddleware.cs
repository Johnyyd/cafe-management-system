using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CafeManagement.Api.Middleware;

/// <summary>
/// Middleware to monitor and log performance metrics for HTTP requests.
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private static readonly ActivitySource ActivitySource = new ActivitySource("CafeManagement.Api");

    public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start activity for request tracking
        using var activity = ActivitySource.StartActivity("HTTP " + context.Request.Method, ActivityKind.Server);
        activity?.SetTag("http.method", context.Request.Method);
        activity?.SetTag("http.url", context.Request.Path);
        activity?.SetTag("http.scheme", context.Request.Scheme);
        activity?.SetTag("http.host", context.Request.Host.Value);

        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Add custom header with response time before response starts
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Response-Time-Ms"] = stopwatch.Elapsed.TotalMilliseconds.ToString("F2");
            return Task.CompletedTask;
        });

        // Log request start
        _logger.LogInformation("Request started: {Method} {Path} from {RemoteIpAddress}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress);

        try
        {
            // Process request
            await _next(context);

            // Stop timing
            stopwatch.Stop();

            // Get response status code
            var statusCode = context.Response.StatusCode;

            // Set activity tags
            activity?.SetTag("http.status_code", statusCode);
            activity?.SetTag("http.duration_ms", stopwatch.Elapsed.TotalMilliseconds);

            // Record metrics
            var metricsService = context.RequestServices.GetRequiredService<Services.IPerformanceMetricsService>();
            metricsService.RecordTiming("http.request.duration", stopwatch.Elapsed);
            metricsService.RecordMetric("http.request.count", 1, new Dictionary<string, string>
            {
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path,
                ["status_code"] = statusCode.ToString()
            });

            // Log request completion with timing
            _logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            // Stop timing even on exception
            stopwatch.Stop();

            // Set activity tags for error
            activity?.SetTag("http.status_code", 500);
            activity?.SetTag("http.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            // Record error metrics
            var metricsService = context.RequestServices.GetRequiredService<Services.IPerformanceMetricsService>();
            metricsService.RecordTiming("http.request.duration", stopwatch.Elapsed);
            metricsService.RecordMetric("http.request.count", 1, new Dictionary<string, string>
            {
                ["method"] = context.Request.Method,
                ["path"] = context.Request.Path,
                ["status_code"] = "500",
                ["error_type"] = ex.GetType().Name
            });

            // Log exception with timing
            _logger.LogError(ex, "Request failed: {Method} {Path} after {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.Elapsed.TotalMilliseconds);

            throw; // Re-throw to let error handling middleware process it
        }
    }
}