using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Strategy.AnomalyDetection;

/// <summary>
/// Time-based anomaly detection using moving averages and trend analysis
/// </summary>
public class TimeSeriesAnomalyDetectionStrategy : IAnomalyDetectionStrategy
{
    private readonly ILogger<TimeSeriesAnomalyDetectionStrategy> _logger;

    public string StrategyName => "TimeSeriesAnomalyDetection";
    public double Threshold { get; set; } = 0.3; // 30% deviation from moving average

    public TimeSeriesAnomalyDetectionStrategy(ILogger<TimeSeriesAnomalyDetectionStrategy> logger)
    {
        _logger = logger;
    }    public Task<AnomalyDetectionResult?> DetectAsync(
        SensorData sensorData, 
        IEnumerable<SensorData> historicalData, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var recentData = historicalData
                .Where(x => x.SensorType == sensorData.SensorType)
                .Where(x => x.Timestamp >= DateTime.UtcNow.AddHours(-24)) // Last 24 hours
                .OrderBy(x => x.Timestamp)
                .ToList();            if (recentData.Count < 5)
            {                return Task.FromResult<AnomalyDetectionResult?>(new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = AnomalyType.NetworkTimeout,
                    Severity = 0.0,
                    Description = "Insufficient recent data for time series analysis",
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["RecentDataPoints"] = recentData.Count
                    }
                });
            }

            // Calculate exponential moving average
            var alpha = 0.3; // Smoothing factor
            var ema = recentData.First().Value.Value;
            
            foreach (var data in recentData.Skip(1))
            {
                ema = alpha * data.Value.Value + (1 - alpha) * ema;
            }

            // Calculate trend
            var trendWindow = Math.Min(10, recentData.Count);
            var trendData = recentData.TakeLast(trendWindow).ToList();
            var trend = CalculateTrend(trendData);

            // Predict expected value based on trend
            var expectedValue = ema + trend;
            var deviation = Math.Abs(sensorData.Value.Value - expectedValue) / Math.Max(Math.Abs(expectedValue), 1.0);
            
            var isAnomaly = deviation > Threshold;
            var confidence = Math.Min(deviation / Threshold, 1.0);            _logger.LogDebug("Time series anomaly detection for {SensorType}: Deviation={Deviation}, Threshold={Threshold}, IsAnomaly={IsAnomaly}", 
                sensorData.SensorType, deviation, Threshold, isAnomaly);            if (isAnomaly)
            {
                var anomalyType = sensorData.Value.Value > expectedValue ? AnomalyType.SuddenSpike : AnomalyType.SuddenDrop;
                var severity = Math.Min(confidence, 1.0);

                return Task.FromResult<AnomalyDetectionResult?>(new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = anomalyType,
                    Severity = severity,
                    Description = $"Time series anomaly detected: EMA={ema:F2}, Expected={expectedValue:F2}, Actual={sensorData.Value.Value:F2}, Deviation={deviation:P2}",
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["ExponentialMovingAverage"] = ema,
                        ["Trend"] = trend,
                        ["ExpectedValue"] = expectedValue,
                        ["Deviation"] = deviation,
                        ["RecentDataPoints"] = recentData.Count
                    }
                });
            }// No anomaly detected
            return Task.FromResult<AnomalyDetectionResult?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in time series anomaly detection for sensor {SensorId}", sensorData.Id);
            throw;
        }
    }

    public bool CanHandle(string sensorType)
    {
        // Time series analysis works best for continuous sensors
        var continuousSensorTypes = new[] { "temperature", "humidity", "pressure", "flow", "voltage", "current" };
        return continuousSensorTypes.Any(type => sensorType.ToLowerInvariant().Contains(type));
    }

    private double CalculateTrend(List<SensorData> data)
    {
        if (data.Count < 2) return 0;

        var n = data.Count;
        var sumX = 0.0;
        var sumY = 0.0;
        var sumXY = 0.0;
        var sumX2 = 0.0;

        for (int i = 0; i < n; i++)
        {
            var x = i;
            var y = data[i].Value.Value;
            
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        // Linear regression slope (trend)
        var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return double.IsNaN(slope) ? 0 : slope;
    }
}
