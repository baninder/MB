using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Strategy.AnomalyDetection;

/// <summary>
/// Threshold-based anomaly detection with configurable upper and lower bounds
/// </summary>
public class ThresholdAnomalyDetectionStrategy : IAnomalyDetectionStrategy
{
    private readonly ILogger<ThresholdAnomalyDetectionStrategy> _logger;
    
    public string StrategyName => "ThresholdAnomalyDetection";
    public double Threshold { get; set; } = 1.0; // Not used directly in this strategy

    // Configurable thresholds per sensor type
    private readonly Dictionary<string, (double Min, double Max)> _sensorThresholds = new()
    {
        ["temperature"] = (-40, 80),
        ["humidity"] = (0, 100),
        ["pressure"] = (800, 1200),
        ["voltage"] = (0, 12),
        ["current"] = (0, 10),
        ["ph"] = (0, 14),
        ["co2"] = (350, 5000)
    };

    public ThresholdAnomalyDetectionStrategy(ILogger<ThresholdAnomalyDetectionStrategy> logger)
    {
        _logger = logger;
    }

    public async Task<AnomalyDetectionResult?> DetectAsync(
        SensorData sensorData, 
        IEnumerable<SensorData> historicalData, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sensorTypeKey = sensorData.SensorType.ToLowerInvariant();
            
            if (!_sensorThresholds.TryGetValue(sensorTypeKey, out var thresholds))
            {
                // For unknown sensor types, use dynamic thresholds based on historical data
                return await CalculateDynamicThresholds(sensorData, historicalData);
            }

            var value = sensorData.Value.Value;
            var isBelow = value < thresholds.Min;
            var isAbove = value > thresholds.Max;
            var isAnomaly = isBelow || isAbove;

            var confidence = 1.0;
            if (isBelow)
            {
                confidence = Math.Min((thresholds.Min - value) / Math.Abs(thresholds.Min), 1.0);
            }
            else if (isAbove)
            {
                confidence = Math.Min((value - thresholds.Max) / thresholds.Max, 1.0);
            }

            var message = isAnomaly 
                ? $"Value {value:F2} is {(isBelow ? "below" : "above")} threshold range [{thresholds.Min:F2}, {thresholds.Max:F2}]"
                : $"Value {value:F2} is within threshold range [{thresholds.Min:F2}, {thresholds.Max:F2}]";            _logger.LogDebug("Threshold anomaly detection for {SensorType}: Value={Value}, Range=[{Min}, {Max}], IsAnomaly={IsAnomaly}", 
                sensorData.SensorType, value, thresholds.Min, thresholds.Max, isAnomaly);

            if (isAnomaly)
            {
                var anomalyType = isBelow ? AnomalyType.SuddenDrop : AnomalyType.SuddenSpike;
                
                return new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = anomalyType,
                    Severity = confidence,
                    Description = message,
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["MinThreshold"] = thresholds.Min,
                        ["MaxThreshold"] = thresholds.Max,
                        ["IsBelow"] = isBelow,
                        ["IsAbove"] = isAbove
                    }
                };
            }

            // No anomaly detected
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in threshold anomaly detection for sensor {SensorId}", sensorData.Id);
            throw;
        }
    }

    public bool CanHandle(string sensorType)
    {
        return !string.IsNullOrEmpty(sensorType);
    }

    public void SetThresholds(string sensorType, double min, double max)
    {
        _sensorThresholds[sensorType.ToLowerInvariant()] = (min, max);
    }    private Task<AnomalyDetectionResult> CalculateDynamicThresholds(
        SensorData sensorData, 
        IEnumerable<SensorData> historicalData)
    {
        var recentData = historicalData
            .Where(x => x.SensorType == sensorData.SensorType)
            .Where(x => x.Timestamp >= DateTime.UtcNow.AddDays(-7)) // Last 7 days
            .Select(x => x.Value.Value)
            .ToList();

        if (recentData.Count < 10)
        {
            return Task.FromResult(new AnomalyDetectionResult
            {
                DeviceId = sensorData.DeviceId,
                SensorType = sensorData.SensorType,
                Type = AnomalyType.NetworkTimeout,
                Severity = 0.0,
                Description = "Insufficient historical data for dynamic threshold calculation",
                OriginalData = sensorData,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Method"] = StrategyName + "_Dynamic",
                    ["HistoricalSamples"] = recentData.Count
                }
            });
        }

        var sorted = recentData.OrderBy(x => x).ToList();
        var q1 = sorted[(int)(sorted.Count * 0.25)];
        var q3 = sorted[(int)(sorted.Count * 0.75)];
        var iqr = q3 - q1;
        
        var lowerBound = q1 - 1.5 * iqr;
        var upperBound = q3 + 1.5 * iqr;
        
        var value = sensorData.Value.Value;
        var isAnomaly = value < lowerBound || value > upperBound;
        var confidence = isAnomaly ? 0.8 : 0.0; // Lower confidence for dynamic thresholds
        
        if (isAnomaly)
        {
            var anomalyType = value < lowerBound ? AnomalyType.SuddenDrop : AnomalyType.SuddenSpike;
            
            return Task.FromResult(new AnomalyDetectionResult
            {
                DeviceId = sensorData.DeviceId,
                SensorType = sensorData.SensorType,
                Type = anomalyType,
                Severity = confidence,
                Description = $"Dynamic threshold analysis: Value {value:F2}, Range [{lowerBound:F2}, {upperBound:F2}]",
                OriginalData = sensorData,
                AdditionalInfo = new Dictionary<string, object>
                {
                    ["Method"] = StrategyName + "_Dynamic",
                    ["DynamicLowerBound"] = lowerBound,
                    ["DynamicUpperBound"] = upperBound,
                    ["Q1"] = q1,
                    ["Q3"] = q3,
                    ["IQR"] = iqr,
                    ["HistoricalSamples"] = recentData.Count
                }
            });
        }
        
        // No anomaly detected
        return Task.FromResult(new AnomalyDetectionResult
        {
            DeviceId = sensorData.DeviceId,
            SensorType = sensorData.SensorType,
            Type = AnomalyType.OutOfRange,
            Severity = 0.0,
            Description = $"Value {value:F2} is within dynamic range [{lowerBound:F2}, {upperBound:F2}]",
            OriginalData = sensorData,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["Method"] = StrategyName + "_Dynamic",
                ["DynamicLowerBound"] = lowerBound,
                ["DynamicUpperBound"] = upperBound,
                ["Q1"] = q1,
                ["Q3"] = q3,
                ["IQR"] = iqr,
                ["HistoricalSamples"] = recentData.Count
            }
        });
    }
}
