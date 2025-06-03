using IoTSensorDataProcessor.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IoTSensorDataProcessor.Infrastructure.Services;

public class SensorDataBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SensorDataBackgroundService> _logger;
    private IMqttSensorDataService? _mqttService;

    public SensorDataBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SensorDataBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor Data Background Service started");

        try
        {
            // Get MQTT service
            _mqttService = _serviceProvider.GetRequiredService<IMqttSensorDataService>();
            
            // Start MQTT client
            await _mqttService.StartAsync(stoppingToken);

            // Process incoming sensor data
            await ProcessSensorDataAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Sensor Data Background Service");
        }
    }

    private async Task ProcessSensorDataAsync(CancellationToken stoppingToken)
    {
        if (_mqttService == null) return;

        var dataChannel = _mqttService.GetDataChannel();

        await foreach (var sensorData in dataChannel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<ISensorDataProcessor>();
                
                await processor.ProcessAsync(sensorData, stoppingToken);
                
                _logger.LogDebug("Processed sensor data from device {DeviceId}, sensor {SensorType}, value {Value}",
                    sensorData.DeviceId, sensorData.SensorType, sensorData.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sensor Data Background Service stopping");
        
        if (_mqttService != null)
        {
            await _mqttService.StopAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
