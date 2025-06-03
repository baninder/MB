using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;

namespace IoTSensorDataProcessor.Infrastructure.Services;

public class SimpleAnomalyDetectionService : IAnomalyDetectionService
{
    private readonly Dictionary<string, List<SensorReading>> _sensorHistory;
    private readonly Dictionary<string, SensorThresholds> _thresholds;
    private readonly int _historySize;

    public SimpleAnomalyDetectionService(int historySize = 100)
    {
        _sensorHistory = new Dictionary<string, List<SensorReading>>();
        _thresholds = new Dictionary<string, SensorThresholds>();
        _historySize = historySize;
        InitializeDefaultThresholds();
    }

    public async Task<AnomalyDetectionResult?> DetectAnomalyAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make method async
        
        var key = $"{sensorData.DeviceId}_{sensorData.SensorType}";
        
        // Initialize history for new sensor
        if (!_sensorHistory.ContainsKey(key))
        {
            _sensorHistory[key] = new List<SensorReading>();
        }        var history = _sensorHistory[key];
        var reading = new SensorReading(sensorData.Value.Value, sensorData.Timestamp);
        
        // Detect anomalies before adding to history
        var anomaly = DetectAnomaly(sensorData, history);
        
        // Add to history and maintain size
        history.Add(reading);
        if (history.Count > _historySize)
        {
            history.RemoveAt(0);
        }

        return anomaly;
    }

    public async Task<IEnumerable<AnomalyDetectionResult>> GetAnomaliesAsync(
        string? deviceId = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make method async
        
        // In a real implementation, this would query a database
        // For now, return empty list as this is a stateless detection service
        return new List<AnomalyDetectionResult>();
    }

    private AnomalyDetectionResult? DetectAnomaly(SensorData sensorData, List<SensorReading> history)
    {
        if (history.Count < 2) return null;        var thresholds = GetThresholds(sensorData.SensorType);
        var currentValue = sensorData.Value.Value;
        var previousValue = history.Last().Value;

        // Check for out of range values
        if (currentValue < thresholds.MinValue || currentValue > thresholds.MaxValue)
        {
            return new AnomalyDetectionResult
            {
                DeviceId = sensorData.DeviceId,
                SensorType = sensorData.SensorType,
                Type = AnomalyType.OutOfRange,
                Severity = CalculateSeverity(currentValue, thresholds.MinValue, thresholds.MaxValue),
                Description = $"Value {currentValue} is outside normal range [{thresholds.MinValue}, {thresholds.MaxValue}]",
                OriginalData = sensorData
            };
        }

        // Check for sudden spikes
        var percentageChange = Math.Abs((currentValue - previousValue) / previousValue * 100);
        if (percentageChange > thresholds.MaxPercentageChange)
        {
            var anomalyType = currentValue > previousValue ? AnomalyType.SuddenSpike : AnomalyType.SuddenDrop;
            return new AnomalyDetectionResult
            {
                DeviceId = sensorData.DeviceId,
                SensorType = sensorData.SensorType,
                Type = anomalyType,
                Severity = Math.Min(percentageChange / 100.0, 1.0),
                Description = $"Sudden {(anomalyType == AnomalyType.SuddenSpike ? "spike" : "drop")} detected: {percentageChange:F1}% change",
                OriginalData = sensorData
            };
        }

        // Check for flat line (no variation)
        if (history.Count >= 10)
        {
            var recentValues = history.TakeLast(10).Select(r => r.Value);
            var variance = CalculateVariance(recentValues);
            if (variance < thresholds.MinVariance)
            {
                return new AnomalyDetectionResult
                {
                    DeviceId = sensorData.DeviceId,
                    SensorType = sensorData.SensorType,
                    Type = AnomalyType.FlatLine,
                    Severity = 0.5,
                    Description = "Sensor appears to be stuck (no variation detected)",
                    OriginalData = sensorData
                };
            }
        }

        return null;
    }

    private SensorThresholds GetThresholds(string sensorType)
    {
        return _thresholds.TryGetValue(sensorType.ToLower(), out var thresholds) 
            ? thresholds 
            : _thresholds["default"];
    }

    private static double CalculateSeverity(double value, double min, double max)
    {
        var range = max - min;
        if (value < min)
        {
            return Math.Min((min - value) / range, 1.0);
        }
        return Math.Min((value - max) / range, 1.0);
    }

    private static double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count < 2) return 0;

        var average = valuesList.Average();
        var sumOfSquares = valuesList.Sum(v => Math.Pow(v - average, 2));
        return sumOfSquares / valuesList.Count;
    }

    private void InitializeDefaultThresholds()
    {
        _thresholds["temperature"] = new SensorThresholds(-40, 85, 50, 0.1);
        _thresholds["humidity"] = new SensorThresholds(0, 100, 30, 0.1);
        _thresholds["pressure"] = new SensorThresholds(300, 1100, 20, 0.1);
        _thresholds["default"] = new SensorThresholds(double.MinValue, double.MaxValue, 100, 0.001);
    }

    private record SensorReading(double Value, DateTime Timestamp);
    
    private record SensorThresholds(double MinValue, double MaxValue, double MaxPercentageChange, double MinVariance);
}
