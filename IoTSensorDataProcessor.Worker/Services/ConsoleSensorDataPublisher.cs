using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Worker.Services;

public class ConsoleSensorDataPublisher : ISensorDataPublisher
{
    private readonly ILogger<ConsoleSensorDataPublisher> _logger;

    public ConsoleSensorDataPublisher(ILogger<ConsoleSensorDataPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing sensor data: DeviceId={DeviceId}, SensorType={SensorType}, Value={Value}, Timestamp={Timestamp}",
            sensorData.DeviceId, sensorData.SensorType, sensorData.Value, sensorData.Timestamp);
        return Task.CompletedTask;
    }

    public Task PublishAnomalyAsync(AnomalyDetectionResult anomaly, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Publishing anomaly: DeviceId={DeviceId}, Type={Type}, Severity={Severity}, Description={Description}",
            anomaly.DeviceId, anomaly.Type, anomaly.Severity, anomaly.Description);
        return Task.CompletedTask;
    }
}
