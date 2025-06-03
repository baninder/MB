using IoTSensorDataProcessor.Core.Interfaces.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Messaging;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Models;
using IoTSensorDataProcessor.Core.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorDataProcessor.Api.Controllers;

/// <summary>
/// Demonstrates advanced design patterns and Spark integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdvancedPatternsController : ControllerBase
{
    private readonly IAnomalyDetectionStrategyFactory _anomalyStrategyFactory;
    private readonly ISerializationStrategyFactory _serializationStrategyFactory;
    private readonly IProcessingStrategyFactory<SensorData, SensorData> _processingStrategyFactory;
    private readonly ISparkMessageBroker<SensorData> _sparkBroker;
    private readonly ILogger<AdvancedPatternsController> _logger;

    public AdvancedPatternsController(
        IAnomalyDetectionStrategyFactory anomalyStrategyFactory,
        ISerializationStrategyFactory serializationStrategyFactory,
        IProcessingStrategyFactory<SensorData, SensorData> processingStrategyFactory,
        ISparkMessageBroker<SensorData> sparkBroker,
        ILogger<AdvancedPatternsController> logger)
    {
        _anomalyStrategyFactory = anomalyStrategyFactory;
        _serializationStrategyFactory = serializationStrategyFactory;
        _processingStrategyFactory = processingStrategyFactory;
        _sparkBroker = sparkBroker;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates Strategy pattern for anomaly detection
    /// </summary>
    [HttpPost("detect-anomaly/{strategy}")]
    public async Task<IActionResult> DetectAnomaly(string strategy, [FromBody] SensorData sensorData)
    {
        try
        {
            var anomalyStrategy = _anomalyStrategyFactory.CreateStrategy(strategy);
            var historicalData = GenerateHistoricalData(sensorData.SensorType, 100);
            
            var result = await anomalyStrategy.DetectAsync(sensorData, historicalData);
              _logger.LogInformation("Anomaly detection completed using {Strategy}: HasResult={HasResult}, Severity={Severity}", 
                strategy, result != null, result?.Severity);

            return Ok(new
            {
                Strategy = strategy,
                Result = result,
                AvailableStrategies = _anomalyStrategyFactory.GetAllStrategies().Select(s => s.StrategyName)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in anomaly detection with strategy {Strategy}", strategy);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates Factory pattern for creating strategies
    /// </summary>
    [HttpGet("strategies/anomaly")]
    public IActionResult GetAnomalyStrategies()
    {
        try
        {
            var strategies = _anomalyStrategyFactory.GetAllStrategies().Select(s => new
            {
                Name = s.StrategyName,
                Threshold = s.Threshold,
                CanHandle = new[] { "temperature", "humidity", "pressure" }.ToDictionary(
                    type => type, 
                    type => s.CanHandle(type))
            });

            return Ok(strategies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomaly strategies");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates data processing pipeline with multiple strategies
    /// </summary>
    [HttpPost("process-data")]
    public async Task<IActionResult> ProcessData([FromBody] SensorData sensorData)
    {
        try
        {
            var orderedStrategies = _processingStrategyFactory.GetOrderedStrategies();
            var processedData = sensorData;

            var processingResults = new List<object>();

            foreach (var strategy in orderedStrategies)
            {
                if (strategy.CanProcess(processedData))
                {
                    var originalValue = processedData.Value.Value;
                    processedData = await strategy.ProcessAsync(processedData);
                    
                    processingResults.Add(new
                    {
                        Strategy = strategy.StrategyName,
                        Priority = strategy.Priority,
                        OriginalValue = originalValue,
                        ProcessedValue = processedData.Value.Value,
                        MetadataCount = processedData.Metadata.Count
                    });
                }
            }

            return Ok(new
            {
                OriginalData = sensorData,
                ProcessedData = processedData,
                ProcessingSteps = processingResults
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in data processing pipeline");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates serialization strategies
    /// </summary>
    [HttpPost("serialize/{format}")]
    public async Task<IActionResult> SerializeData(string format, [FromBody] SensorData sensorData)
    {
        try
        {
            var strategy = _serializationStrategyFactory.CreateStrategy<SensorData>(format);
            var serialized = await strategy.SerializeAsync(sensorData);
            
            // Also demonstrate deserialization
            var deserialized = await strategy.DeserializeAsync(serialized);

            return Ok(new
            {
                Format = format,
                ContentType = strategy.ContentType,
                SerializedData = serialized,
                DeserializedMatch = deserialized?.Id == sensorData.Id,
                SerializedLength = serialized.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in serialization with format {Format}", format);
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Demonstrates Spark integration for big data processing
    /// </summary>
    [HttpPost("spark/process-batch")]
    public async Task<IActionResult> ProcessWithSpark([FromBody] SensorData[] sensorDataBatch)
    {        try
        {
            // Create DataFrame from sensor data
            var dataFrame = await _sparkBroker.CreateDataFrameAsync(sensorDataBatch);
            
            // Get DataFrame count
            var count = await dataFrame.CountAsync();
            
            _logger.LogInformation("Created Spark DataFrame with {Count} rows", count);            // Demonstrate DataFrame operations
            var filteredData = dataFrame.Filter(data => data.Value.Value > 20); // Example filter
            var filteredResults = await filteredData.CollectAsync();
            var filteredCount = await filteredData.CountAsync();
            
            // Publish to Spark streaming (demonstration)
            await _sparkBroker.PublishBatchAsync(sensorDataBatch, "sensor-data-topic");            return Ok(new
            {
                OriginalCount = sensorDataBatch.Length,
                DataFrameInfo = new
                {
                    TotalRows = count,
                    FilteredRows = filteredCount,
                    SampleFilteredData = filteredResults.Take(5)
                },
                Message = "Batch processed successfully with Spark"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Spark batch processing");
            return StatusCode(500, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Health check for advanced patterns and Spark integration
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        try
        {
            var health = new
            {
                Timestamp = DateTime.UtcNow,
                Patterns = new
                {
                    AnomalyStrategies = _anomalyStrategyFactory.GetAllStrategies().Count(),
                    ProcessingStrategies = _processingStrategyFactory.GetAllStrategies().Count(),
                    SerializationFormats = new[] { "application/json", "application/xml" }
                },
                Spark = new
                {
                    Available = true, // Would check actual Spark connection in production
                    Message = "Spark integration configured"
                },
                Status = "Healthy"
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
        }
    }

    private IEnumerable<SensorData> GenerateHistoricalData(string sensorType, int count)
    {
        var random = new Random();
        var data = new List<SensorData>();
        var baseValue = sensorType.ToLowerInvariant() switch
        {
            "temperature" => 22.0,
            "humidity" => 45.0,
            "pressure" => 1013.25,
            _ => 50.0
        };        for (int i = 0; i < count; i++)
        {
            var timestamp = DateTime.UtcNow.AddMinutes(-count + i);
            var variance = random.NextDouble() * 10 - 5; // ±5 unit variance
            var value = baseValue + variance;
            
            var sensorData = SensorData.Create(
                $"sensor-{sensorType}-001",
                sensorType,
                value,
                sensorType == "temperature" ? "celsius" : 
                sensorType == "humidity" ? "percent" :
                sensorType == "pressure" ? "hpa" : "units"
            );
            
            // Set location and timestamp after creation
            sensorData.UpdateLocation(40.7128, -74.0060);
            // Note: Timestamp is already set in Create method, but we can update if needed
            
            data.Add(sensorData);
        }

        return data;
    }
}
