using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Strategy.Processing;

/// <summary>
/// Data filtering and cleaning processing strategy
/// </summary>
public class DataCleaningProcessingStrategy : IProcessingStrategy<SensorData, SensorData>
{
    private readonly ILogger<DataCleaningProcessingStrategy> _logger;

    public string StrategyName => "DataCleaning";
    public int Priority => 1; // High priority - should run first

    public DataCleaningProcessingStrategy(ILogger<DataCleaningProcessingStrategy> logger)
    {
        _logger = logger;
    }    public async Task<SensorData> ProcessAsync(SensorData input, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask; // Make async
            
            // Create a copy to avoid modifying the original
            var cleanedData = SensorData.Create(
                input.DeviceId,
                input.SensorType,
                CleanValue(input.Value.Value, input.SensorType),
                input.Value.Unit
            );

            // Copy timestamp and location if available
            if (input.Location != null)
            {
                cleanedData.UpdateLocation(input.Location.Latitude, input.Location.Longitude, input.Location.Altitude);
            }

            // Apply additional cleaning rules
            cleanedData = ApplyCleaningRules(cleanedData);

            _logger.LogDebug("Data cleaning applied to sensor {DeviceId}: {OriginalValue} -> {CleanedValue}", 
                input.DeviceId, input.Value.Value, cleanedData.Value.Value);

            return cleanedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data cleaning for sensor {DeviceId}", input.DeviceId);
            throw;
        }
    }

    public bool CanProcess(SensorData input)
    {
        return input != null && input.Value != null;
    }

    private double CleanValue(double value, string sensorType)
    {
        // Remove obvious outliers based on sensor type
        return sensorType.ToLowerInvariant() switch
        {
            var type when type.Contains("temperature") => ClampValue(value, -100, 150),
            var type when type.Contains("humidity") => ClampValue(value, 0, 100),
            var type when type.Contains("pressure") => ClampValue(value, 0, 2000),
            var type when type.Contains("ph") => ClampValue(value, 0, 14),
            var type when type.Contains("voltage") => ClampValue(value, -50, 50),
            var type when type.Contains("current") => ClampValue(value, -100, 100),
            _ => value // No cleaning for unknown types
        };
    }

    private double ClampValue(double value, double min, double max)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return (min + max) / 2; // Return middle value if invalid
        }

        return Math.Max(min, Math.Min(max, value));
    }    private SensorData ApplyCleaningRules(SensorData data)
    {
        // Apply precision rounding based on sensor type
        var precision = GetPrecision(data.SensorType);
        var roundedValue = Math.Round(data.Value.Value, precision);

        if (Math.Abs(roundedValue - data.Value.Value) > double.Epsilon)
        {
            var newData = SensorData.Create(
                data.DeviceId,
                data.SensorType,
                roundedValue,
                data.Value.Unit
            );
            
            // Copy location if available
            if (data.Location != null)
            {
                newData.UpdateLocation(data.Location.Latitude, data.Location.Longitude, data.Location.Altitude);
            }
            
            return newData;
        }

        return data;
    }

    private int GetPrecision(string sensorType)
    {
        return sensorType.ToLowerInvariant() switch
        {
            var type when type.Contains("temperature") => 2,
            var type when type.Contains("humidity") => 1,
            var type when type.Contains("pressure") => 1,
            var type when type.Contains("ph") => 2,
            var type when type.Contains("voltage") => 3,
            var type when type.Contains("current") => 3,
            _ => 2
        };
    }
}
