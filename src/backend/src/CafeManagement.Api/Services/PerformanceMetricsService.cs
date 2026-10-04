using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace CafeManagement.Api.Services;

/// <summary>
/// Service for collecting and exposing application performance metrics.
/// </summary>
public class PerformanceMetricsService : IPerformanceMetricsService
{
    private readonly ILogger<PerformanceMetricsService> _logger;
    private readonly ConcurrentDictionary<string, MetricData> _metrics;
    private Timer _cleanupTimer;

    public PerformanceMetricsService(ILogger<PerformanceMetricsService> logger)
    {
        _logger = logger;
        _metrics = new ConcurrentDictionary<string, MetricData>();

        // Start cleanup timer to remove old metrics every 5 minutes
        _cleanupTimer = new Timer(CleanupOldMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Records a metric measurement.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    public void RecordMetric(string name, double value, IDictionary<string, string>? tags = null)
    {
        var key = GenerateMetricKey(name, tags);
        var metric = _metrics.GetOrAdd(key, _ => new MetricData(name));

        lock (metric)
        {
            metric.AddValue(value, tags);
        }

        _logger.LogDebug("Recorded metric {Name}: {Value}", name, value);
    }

    /// <summary>
    /// Records a timing metric.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="duration">The duration to record.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    public void RecordTiming(string name, TimeSpan duration, IDictionary<string, string>? tags = null)
    {
        RecordMetric(name, duration.TotalMilliseconds, tags);
    }

    /// <summary>
    /// Gets all current metrics.
    /// </summary>
    /// <returns>A dictionary of metric names to their current values.</returns>
    public IDictionary<string, MetricSnapshot> GetMetrics()
    {
        var result = new Dictionary<string, MetricSnapshot>();

        foreach (var kvp in _metrics)
        {
            lock (kvp.Value)
            {
                result[kvp.Key] = kvp.Value.GetSnapshot();
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a specific metric by name and tags.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="tags">Optional tags for the metric.</param>
    /// <returns>The metric snapshot if found, null otherwise.</returns>
    public MetricSnapshot? GetMetric(string name, IDictionary<string, string>? tags = null)
    {
        var key = GenerateMetricKey(name, tags);

        if (_metrics.TryGetValue(key, out var metric))
        {
            lock (metric)
            {
                return metric.GetSnapshot();
            }
        }

        return null;
    }

    private static string GenerateMetricKey(string name, IDictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;

        var tagString = string.Join(",", tags.OrderBy(kvp => kvp.Key)
                                          .Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}[{tagString}]";
    }

    private void CleanupOldMetrics(object? state)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(1); // Keep metrics for 1 hour

        var keysToRemove = new List<string>();

        foreach (var kvp in _metrics)
        {
            lock (kvp.Value)
            {
                if (kvp.Value.LastUpdated < cutoffTime)
                {
                    keysToRemove.Add(kvp.Key);
                }
                else
                {
                    // Clean up old values within the metric
                    kvp.Value.CleanupOldValues(cutoffTime);
                }
            }
        }

        foreach (var key in keysToRemove)
        {
            _metrics.TryRemove(key, out _);
        }

        if (keysToRemove.Count > 0)
        {
            _logger.LogDebug("Cleaned up {Count} old metrics", keysToRemove.Count);
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
}

/// <summary>
/// Interface for the performance metrics service.
/// </summary>
public interface IPerformanceMetricsService : IDisposable
{
    void RecordMetric(string name, double value, IDictionary<string, string>? tags = null);
    void RecordTiming(string name, TimeSpan duration, IDictionary<string, string>? tags = null);
    IDictionary<string, MetricSnapshot> GetMetrics();
    MetricSnapshot? GetMetric(string name, IDictionary<string, string>? tags = null);
}

/// <summary>
/// Stores metric data points over time.
/// </summary>
public class MetricData
{
    private readonly List<MetricPoint> _values = new();
    private readonly object _lock = new();
    public string Name { get; }
    public DateTime LastUpdated { get; private set; }

    public MetricData(string name)
    {
        Name = name;
        LastUpdated = DateTime.UtcNow;
    }

    public void AddValue(double value, IDictionary<string, string>? tags = null)
    {
        lock (_lock)
        {
            _values.Add(new MetricPoint
            {
                Value = value,
                Timestamp = DateTime.UtcNow,
                Tags = tags ?? new Dictionary<string, string>()
            });

            LastUpdated = DateTime.UtcNow;
        }
    }

    public void CleanupOldValues(DateTime cutoffTime)
    {
        lock (_lock)
        {
            _values.RemoveAll(v => v.Timestamp < cutoffTime);

            if (_values.Count == 0)
            {
                LastUpdated = DateTime.MinValue;
            }
            else
            {
                LastUpdated = _values.Max(v => v.Timestamp);
            }
        }
    }

    public MetricSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            if (_values.Count == 0)
                return new MetricSnapshot(Name, 0, 0, 0, 0, 0);

            var values = _values.Select(v => v.Value).ToArray();

            return new MetricSnapshot(
                Name,
                values.Count(),
                values.Min(),
                values.Max(),
                values.Average(),
                values.Length > 1 ?
                    Math.Sqrt(values.Average(v => Math.Pow(v - values.Average(), 2))) :
                    0
            );
        }
    }
}

/// <summary>
/// Represents a single metric data point.
/// </summary>
public class MetricPoint
{
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
    public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Snapshot of metric statistics.
/// </summary>
public class MetricSnapshot
{
    public MetricSnapshot(string name, long count, double min, double max, double average, double stdDev)
    {
        Name = name;
        Count = count;
        Min = min;
        Max = max;
        Average = average;
        StdDev = stdDev;
    }

    public string Name { get; }
    public long Count { get; }
    public double Min { get; }
    public double Max { get; }
    public double Average { get; }
    public double StdDev { get; }
}