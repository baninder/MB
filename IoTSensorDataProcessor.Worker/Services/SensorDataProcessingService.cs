using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Worker.Services;

public class SensorDataProcessingService : BackgroundService
{
    private readonly IChannelService _channelService;
    private readonly ISensorDataProcessor _sensorDataProcessor;
    private readonly IMqttSensorDataService _mqttSensorDataService;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly ILogger<SensorDataProcessingService> _logger;

    public SensorDataProcessingService(
        IChannelService channelService,
        ISensorDataProcessor sensorDataProcessor,
        IMqttSensorDataService mqttSensorDataService,
        IAnomalyDetectionService anomalyDetectionService,
        ILogger<SensorDataProcessingService> logger)
    {
        _channelService = channelService;
        _sensorDataProcessor = sensorDataProcessor;
        _mqttSensorDataService = mqttSensorDataService;
        _anomalyDetectionService = anomalyDetectionService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sensor Data Processing Service started");

        // Start MQTT service
        await _mqttSensorDataService.StartAsync();
        _logger.LogInformation("MQTT Service started");

        var processingTasks = new[]
        {
            ProcessSensorDataAsync(stoppingToken),
            ProcessAnomaliesAsync(stoppingToken),
            MonitorChannelHealthAsync(stoppingToken)
        };

        try
        {
            await Task.WhenAll(processingTasks);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Sensor Data Processing Service stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in Sensor Data Processing Service");
            throw;
        }
        finally
        {
            await _mqttSensorDataService.StopAsync();
            _logger.LogInformation("MQTT Service stopped");
        }
    }

    private async Task ProcessSensorDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting sensor data processing loop");

        var channel = await _channelService.GetChannelAsync<SensorData>("sensor-data");
        var reader = channel.Reader;

        await foreach (var sensorData in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                // Process the sensor data through the core processor
                await _sensorDataProcessor.ProcessAsync(sensorData);

                // Publish to MQTT for other consumers
                await _mqttSensorDataService.PublishSensorDataAsync(sensorData);

                _logger.LogDebug("Processed and published sensor data from {DeviceId}", 
                    sensorData.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor data from {DeviceId}", 
                    sensorData.DeviceId);
            }
        }
    }

    private async Task ProcessAnomaliesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting anomaly processing loop");

        var channel = await _channelService.GetChannelAsync<AnomalyDetectionResult>("anomalies");
        var reader = channel.Reader;

        await foreach (var anomaly in reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await HandleAnomalyAsync(anomaly);
                _logger.LogWarning("Processed anomaly: {Severity} for device {DeviceId}", 
                    anomaly.Severity, anomaly.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing anomaly for device {DeviceId}", 
                    anomaly.DeviceId);
            }
        }
    }    private async Task HandleAnomalyAsync(AnomalyDetectionResult anomaly)
    {
        // Publish anomaly to MQTT for alerting systems
        await _mqttSensorDataService.PublishAnomalyAsync(anomaly);

        // Log based on severity
        switch (anomaly.Severity)
        {
            case >= 0.8:
                _logger.LogCritical("CRITICAL ANOMALY: {Description} for device {DeviceId} at {Timestamp}", 
                    anomaly.Description, anomaly.DeviceId, anomaly.DetectedAt);
                break;
            case >= 0.6:
                _logger.LogError("HIGH SEVERITY ANOMALY: {Description} for device {DeviceId}", 
                    anomaly.Description, anomaly.DeviceId);
                break;
            case >= 0.4:
                _logger.LogWarning("MEDIUM SEVERITY ANOMALY: {Description} for device {DeviceId}", 
                    anomaly.Description, anomaly.DeviceId);
                break;
            case >= 0.2:
                _logger.LogInformation("LOW SEVERITY ANOMALY: {Description} for device {DeviceId}", 
                    anomaly.Description, anomaly.DeviceId);
                break;
        }

        // TODO: Add additional anomaly handling logic here
        // - Send alerts to monitoring systems
        // - Trigger automated responses
        // - Store in specialized anomaly database
    }

    private async Task MonitorChannelHealthAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting channel health monitoring");

        var checkInterval = TimeSpan.FromMinutes(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(checkInterval, cancellationToken);

                var sensorDataChannel = await _channelService.GetChannelAsync<SensorData>("sensor-data");
                var anomalyChannel = await _channelService.GetChannelAsync<AnomalyDetectionResult>("anomalies");

                var sensorDataCount = sensorDataChannel.Reader.Count;
                var anomalyCount = anomalyChannel.Reader.Count;

                _logger.LogInformation("Channel Health Check - SensorData Queue: {SensorDataCount}, Anomaly Queue: {AnomalyCount}", 
                    sensorDataCount, anomalyCount);

                // Alert if queues are backing up
                if (sensorDataCount > 1000)
                {
                    _logger.LogWarning("Sensor data channel queue is backing up: {Count} items", sensorDataCount);
                }

                if (anomalyCount > 100)
                {
                    _logger.LogWarning("Anomaly channel queue is backing up: {Count} items", anomalyCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during channel health monitoring");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Sensor Data Processing Service");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Sensor Data Processing Service stopped");
    }
}
