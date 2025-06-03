using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Strategy.Processing;

/// <summary>
/// Data aggregation and enrichment processing strategy
/// </summary>
public class DataEnrichmentProcessingStrategy : IProcessingStrategy<SensorData, SensorData>
{
    private readonly ILogger<DataEnrichmentProcessingStrategy> _logger;

    public string StrategyName => "DataEnrichment";
    public int Priority => 5; // Medium priority

    public DataEnrichmentProcessingStrategy(ILogger<DataEnrichmentProcessingStrategy> logger)
    {
        _logger = logger;
    }    public async Task<SensorData> ProcessAsync(SensorData input, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask; // Make async
            
            var enrichedData = input;

            // Add derived metrics
            enrichedData = AddDerivedMetrics(enrichedData);

            // Add quality indicators
            enrichedData = AddQualityIndicators(enrichedData);

            _logger.LogDebug("Data enrichment applied to sensor {DeviceId}", input.DeviceId);

            return enrichedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data enrichment for sensor {DeviceId}", input.DeviceId);
            throw;
        }
    }

    public bool CanProcess(SensorData input)
    {
        return input != null;
    }    private SensorData AddDerivedMetrics(SensorData data)
    {
        // Add computed metrics based on sensor type
        switch (data.SensorType.ToLowerInvariant())
        {
            case var type when type.Contains("temperature"):
                if (data.Value.Unit.ToLowerInvariant() == "celsius")
                {
                    data.AddMetadata("TemperatureFahrenheit", (data.Value.Value * 9.0 / 5.0) + 32);
                    data.AddMetadata("TemperatureKelvin", data.Value.Value + 273.15);
                }
                break;

            case var type when type.Contains("pressure"):
                if (data.Value.Unit.ToLowerInvariant() == "hpa")
                {
                    data.AddMetadata("PressurePsi", data.Value.Value * 0.0145038);
                    data.AddMetadata("PressureInHg", data.Value.Value * 0.02953);
                }
                break;
        }

        // Add temporal context
        data.AddMetadata("HourOfDay", data.Timestamp.Hour);
        data.AddMetadata("DayOfWeek", data.Timestamp.DayOfWeek.ToString());
        data.AddMetadata("IsWeekend", data.Timestamp.DayOfWeek == DayOfWeek.Saturday || 
                                     data.Timestamp.DayOfWeek == DayOfWeek.Sunday);

        return data;
    }    private SensorData AddQualityIndicators(SensorData data)
    {
        // Calculate data quality score
        var qualityScore = CalculateQualityScore(data);
        data.AddMetadata("QualityScore", qualityScore);
        data.AddMetadata("QualityLevel", GetQualityLevel(qualityScore));

        // Add data freshness indicator
        var dataAge = DateTime.UtcNow - data.Timestamp;
        data.AddMetadata("DataAgeMinutes", dataAge.TotalMinutes);
        data.AddMetadata("IsFresh", dataAge.TotalMinutes < 5); // Consider data fresh if less than 5 minutes old

        return data;
    }

    private double CalculateQualityScore(SensorData data)
    {
        double score = 1.0;

        // Deduct points for various quality issues
        if (double.IsNaN(data.Value.Value) || double.IsInfinity(data.Value.Value))
            score -= 0.5;

        // Check if value is within expected range
        if (!IsValueInExpectedRange(data))
            score -= 0.2;

        // Check data freshness
        var dataAge = DateTime.UtcNow - data.Timestamp;
        if (dataAge.TotalMinutes > 10)
            score -= 0.1;
        if (dataAge.TotalMinutes > 60)
            score -= 0.2;

        return Math.Max(0, score);
    }

    private string GetQualityLevel(double score)
    {
        return score switch
        {
            >= 0.9 => "Excellent",
            >= 0.7 => "Good",
            >= 0.5 => "Fair",
            >= 0.3 => "Poor",
            _ => "Critical"
        };
    }

    private bool IsValueInExpectedRange(SensorData data)
    {
        var value = data.Value.Value;
        
        return data.SensorType.ToLowerInvariant() switch
        {
            var type when type.Contains("temperature") => value >= -50 && value <= 100,
            var type when type.Contains("humidity") => value >= 0 && value <= 100,
            var type when type.Contains("pressure") => value >= 800 && value <= 1200,
            var type when type.Contains("ph") => value >= 0 && value <= 14,
            _ => true // Unknown types assumed to be valid
        };
    }
}
