using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IoTSensorDataProcessor.Worker.Services;

public class SystemHealthMonitorService : BackgroundService
{
    private readonly ILogger<SystemHealthMonitorService> _logger;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;

    public SystemHealthMonitorService(ILogger<SystemHealthMonitorService> logger)
    {
        _logger = logger;
        
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize performance counters. System metrics will be limited.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("System Health Monitor Service started");

        var monitoringInterval = TimeSpan.FromMinutes(5);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(monitoringInterval, stoppingToken);
                await CollectAndLogSystemMetrics();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system health monitoring");
            }
        }
    }

    private async Task CollectAndLogSystemMetrics()
    {
        var metrics = new Dictionary<string, object>();

        try
        {
            // Memory metrics
            var process = Process.GetCurrentProcess();
            var workingSet = process.WorkingSet64;
            var privateMemory = process.PrivateMemorySize64;
            
            metrics["WorkingSetMB"] = Math.Round(workingSet / 1024.0 / 1024.0, 2);
            metrics["PrivateMemoryMB"] = Math.Round(privateMemory / 1024.0 / 1024.0, 2);
            
            // CPU metrics (if available)
            if (_cpuCounter != null)
            {
                try
                {
                    // First call returns 0, so we wait a moment
                    _cpuCounter.NextValue();
                    await Task.Delay(1000);
                    var cpuUsage = _cpuCounter.NextValue();
                    metrics["CPUUsagePercent"] = Math.Round(cpuUsage, 2);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not collect CPU usage");
                }
            }

            // Available memory (if available)
            if (_memoryCounter != null)
            {
                try
                {
                    var availableMemory = _memoryCounter.NextValue();
                    metrics["AvailableMemoryMB"] = Math.Round(availableMemory, 2);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not collect available memory");
                }
            }

            // Thread metrics
            metrics["ThreadCount"] = process.Threads.Count;
            metrics["HandleCount"] = process.HandleCount;

            // GC metrics
            metrics["Gen0Collections"] = GC.CollectionCount(0);
            metrics["Gen1Collections"] = GC.CollectionCount(1);
            metrics["Gen2Collections"] = GC.CollectionCount(2);
            metrics["TotalMemoryMB"] = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2);

            // Log the metrics
            var metricsString = string.Join(", ", metrics.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            _logger.LogInformation("System Health Metrics - {Metrics}", metricsString);

            // Check for concerning values and alert
            await CheckForAlerts(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
        }
    }

    private async Task CheckForAlerts(Dictionary<string, object> metrics)
    {
        var alerts = new List<string>();

        // Check memory usage
        if (metrics.TryGetValue("WorkingSetMB", out var workingSetObj) && 
            workingSetObj is double workingSet && workingSet > 1000) // > 1GB
        {
            alerts.Add($"High memory usage: {workingSet:F2} MB");
        }

        // Check CPU usage
        if (metrics.TryGetValue("CPUUsagePercent", out var cpuObj) && 
            cpuObj is double cpu && cpu > 80) // > 80%
        {
            alerts.Add($"High CPU usage: {cpu:F2}%");
        }

        // Check available memory
        if (metrics.TryGetValue("AvailableMemoryMB", out var availableMemoryObj) && 
            availableMemoryObj is double availableMemory && availableMemory < 500) // < 500MB
        {
            alerts.Add($"Low available memory: {availableMemory:F2} MB");
        }

        // Check thread count
        if (metrics.TryGetValue("ThreadCount", out var threadCountObj) && 
            threadCountObj is int threadCount && threadCount > 100)
        {
            alerts.Add($"High thread count: {threadCount}");
        }

        // Log alerts
        foreach (var alert in alerts)
        {
            _logger.LogWarning("SYSTEM ALERT: {Alert}", alert);
        }

        // If we have alerts, we might want to trigger some action
        if (alerts.Count > 0)
        {
            await HandleSystemAlerts(alerts);
        }
    }

    private async Task HandleSystemAlerts(List<string> alerts)
    {
        // TODO: Implement alert handling logic
        // - Send notifications to monitoring systems
        // - Trigger garbage collection if memory is high
        // - Implement circuit breaker patterns
        // - Scale down processing if needed

        _logger.LogInformation("Handling {AlertCount} system alerts", alerts.Count);
        
        // For now, just log and optionally trigger GC
        if (alerts.Any(a => a.Contains("memory")))
        {
            _logger.LogInformation("Triggering garbage collection due to memory alerts");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        await Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping System Health Monitor Service");
        
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("System Health Monitor Service stopped");
    }
}
