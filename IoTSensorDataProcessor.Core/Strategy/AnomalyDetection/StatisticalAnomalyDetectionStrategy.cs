using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Strategy.AnomalyDetection;

/// <summary>
/// Statistical anomaly detection using standard deviation and z-score analysis
/// </summary>
public class StatisticalAnomalyDetectionStrategy : IAnomalyDetectionStrategy
{
    private readonly ILogger<StatisticalAnomalyDetectionStrategy> _logger;

    public string StrategyName => "StatisticalAnomalyDetection";
    public double Threshold { get; set; } = 2.5; // Z-score threshold

    public StatisticalAnomalyDetectionStrategy(ILogger<StatisticalAnomalyDetectionStrategy> logger)
    {
        _logger = logger;
    }

    public Task<AnomalyDetectionResult?> DetectAsync(
        SensorData sensorData, 
        IEnumerable<SensorData> historicalData, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var historicalValues = historicalData
                .Where(x => x.SensorType == sensorData.SensorType)
                .Select(x => x.Value.Value)
                .ToList();

            if (historicalValues.Count < 10) // Need minimum historical data
            {
                return Task.FromResult<AnomalyDetectionResult?>(new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = AnomalyType.NetworkTimeout,
                    Severity = 0.0,
                    Description = "Insufficient historical data for statistical analysis",
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["HistoricalCount"] = historicalValues.Count
                    }
                });
            }

            var mean = historicalValues.Average();
            var variance = historicalValues.Sum(x => Math.Pow(x - mean, 2)) / historicalValues.Count;
            var standardDeviation = Math.Sqrt(variance);

            if (standardDeviation == 0)
            {
                return Task.FromResult<AnomalyDetectionResult?>(new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = AnomalyType.FlatLine,
                    Severity = 0.0,
                    Description = "No variance in historical data - flat line detected",
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["Mean"] = mean,
                        ["StandardDeviation"] = standardDeviation
                    }
                });
            }

            var zScore = Math.Abs((sensorData.Value.Value - mean) / standardDeviation);
            var isAnomaly = zScore > Threshold;
            var severity = Math.Min(zScore / Threshold, 1.0);

            _logger.LogDebug("Statistical anomaly detection for {SensorType}: Z-Score={ZScore}, Threshold={Threshold}, IsAnomaly={IsAnomaly}", 
                sensorData.SensorType, zScore, Threshold, isAnomaly);

            if (isAnomaly)
            {
                var anomalyType = sensorData.Value.Value > mean ? AnomalyType.SuddenSpike : AnomalyType.SuddenDrop;
                
                return Task.FromResult<AnomalyDetectionResult?>(new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = anomalyType,
                    Severity = severity,
                    Description = $"Statistical anomaly detected: Z-Score {zScore:F2} exceeds threshold {Threshold:F2}",
                    OriginalData = sensorData,
                    AdditionalInfo = new Dictionary<string, object>
                    {
                        ["Method"] = StrategyName,
                        ["ZScore"] = zScore,
                        ["Mean"] = mean,
                        ["StandardDeviation"] = standardDeviation,
                        ["HistoricalCount"] = historicalValues.Count,
                        ["Threshold"] = Threshold
                    }
                });
            }

            // No anomaly detected
            return Task.FromResult<AnomalyDetectionResult?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in statistical anomaly detection for sensor {DeviceId}", sensorData.DeviceId);
            throw;
        }
    }

    public bool CanHandle(string sensorType)
    {
        // Statistical analysis works for all numeric sensor types
        return !string.IsNullOrEmpty(sensorType);
    }
}
