using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Worker.Services;

public class SensorDataGeneratorService : BackgroundService
{
    private readonly IChannelService _channelService;
    private readonly ILogger<SensorDataGeneratorService> _logger;
    private readonly Random _random = new();
      // Simulated device configurations
    private readonly List<DeviceConfig> _deviceConfigs = new()
    {
        new("TEMP_001", "Temperature Sensor", new Location(40.7128, -74.0060), "temperature"),
        new("TEMP_002", "Temperature Sensor", new Location(40.7130, -74.0062), "temperature"),
        new("HUM_001", "Humidity Sensor", new Location(40.7132, -74.0058), "humidity"),
        new("HUM_002", "Humidity Sensor", new Location(40.7134, -74.0056), "humidity"),
        new("PRES_001", "Pressure Sensor", new Location(40.7125, -74.0065), "pressure"),
        new("PRES_002", "Pressure Sensor", new Location(40.7140, -74.0055), "pressure"),
        new("VIB_001", "Vibration Sensor", new Location(40.7126, -74.0064), "vibration"),
        new("VIB_002", "Vibration Sensor", new Location(40.7138, -74.0052), "vibration")
    };

    public SensorDataGeneratorService(
        IChannelService channelService,
        ILogger<SensorDataGeneratorService> logger)
    {
        _channelService = channelService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor Data Generator Service started");

        var tasks = _deviceConfigs.Select(config => 
            GenerateDataForDevice(config, stoppingToken)).ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sensor Data Generator Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Sensor Data Generator Service");
            throw;
        }
    }

    private async Task GenerateDataForDevice(DeviceConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting data generation for device {DeviceId}", config.DeviceId);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var sensorData = GenerateSensorData(config);
                
                var channel = await _channelService.GetChannelAsync<SensorData>("sensor-data");
                await channel.Writer.WriteAsync(sensorData, cancellationToken);
                  _logger.LogDebug("Generated sensor data for {DeviceId}: {Value} {Unit}", 
                    config.DeviceId, sensorData.Value, sensorData.Value.Unit);

                // Vary the interval slightly to simulate real-world conditions
                var interval = TimeSpan.FromSeconds(5 + (_random.NextDouble() * 5)); // 5-10 seconds
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating data for device {DeviceId}", config.DeviceId);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Brief pause before retry
            }
        }

        _logger.LogInformation("Stopped data generation for device {DeviceId}", config.DeviceId);
    }    private SensorData GenerateSensorData(DeviceConfig config)
    {
        var baseValue = GetBaseValue(config.SensorType);
        var variation = GetVariation(config.SensorType);
        var value = baseValue + (_random.NextDouble() * variation * 2 - variation);
        var roundedValue = Math.Round(value, 2);
        var unit = GetUnit(config.SensorType);

        // Create sensor data using factory method
        var sensorData = SensorData.Create(config.DeviceId, config.SensorType, roundedValue, unit);
          // Set location if available
        if (config.Location != null)
        {
            sensorData.UpdateLocation(config.Location.Latitude, config.Location.Longitude);
        }
        
        // Set quality
        sensorData.UpdateQuality(GenerateQuality());
        
        // Add metadata
        sensorData.AddMetadata("generated", true);
        sensorData.AddMetadata("generator", "SensorDataGeneratorService");
        sensorData.AddMetadata("deviceConfig", config.DeviceId);
        sensorData.AddMetadata("deviceName", config.DeviceName);

        return sensorData;
    }

    private static double GetBaseValue(string sensorType) => sensorType.ToLower() switch
    {
        "temperature" => 22.0, // 22°C base
        "humidity" => 45.0,     // 45% base
        "pressure" => 1013.25,  // 1013.25 hPa base (sea level)
        "vibration" => 0.5,     // 0.5 mm/s base
        _ => 0.0
    };

    private static double GetVariation(string sensorType) => sensorType.ToLower() switch
    {
        "temperature" => 8.0,  // ±8°C variation
        "humidity" => 25.0,    // ±25% variation
        "pressure" => 50.0,    // ±50 hPa variation
        "vibration" => 2.0,    // ±2 mm/s variation
        _ => 1.0
    };

    private static string GetUnit(string sensorType) => sensorType.ToLower() switch
    {
        "temperature" => "°C",
        "humidity" => "%",
        "pressure" => "hPa",
        "vibration" => "mm/s",
        _ => ""
    };

    private SensorQuality GenerateQuality()
    {
        var qualityRandom = _random.NextDouble();
        return qualityRandom switch
        {
            < 0.85 => SensorQuality.Good,
            < 0.95 => SensorQuality.Warning,
            < 0.99 => SensorQuality.Critical,
            _ => SensorQuality.Offline
        };
    }

    private record DeviceConfig(
        string DeviceId,
        string DeviceName,
        Location Location,
        string SensorType);
}
