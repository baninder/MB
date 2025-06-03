using IoTSensorDataProcessor.Core.Models;
using System.Threading.Channels;

namespace IoTSensorDataProcessor.Core.Interfaces;

public interface IChannelService
{
    Task<Channel<T>> GetChannelAsync<T>(string channelName);
    Task<ChannelWriter<T>> GetWriterAsync<T>(string channelName);
    Task<ChannelReader<T>> GetReaderAsync<T>(string channelName);
}

public interface ISensorDataRepository
{
    Task<string> SaveAsync(SensorData sensorData, CancellationToken cancellationToken = default);
    Task<SensorData?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SensorData>> GetByDeviceIdAsync(string deviceId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<SensorData>> GetBySensorTypeAsync(string sensorType, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public interface IAnomalyDetectionService
{
    Task<AnomalyDetectionResult?> DetectAnomalyAsync(SensorData sensorData, CancellationToken cancellationToken = default);
    Task<IEnumerable<AnomalyDetectionResult>> GetAnomaliesAsync(string? deviceId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

public interface IMqttSensorDataService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task PublishSensorDataAsync(SensorData sensorData, CancellationToken cancellationToken = default);
    Task PublishAnomalyAsync(AnomalyDetectionResult anomaly, CancellationToken cancellationToken = default);
    ChannelReader<SensorData> GetDataChannel();
    event EventHandler<SensorData>? SensorDataReceived;
}

public interface ISensorDataProcessor
{
    Task ProcessAsync(SensorData sensorData, CancellationToken cancellationToken = default);
    ChannelReader<SensorData> GetProcessedDataChannel();
}

public interface ISensorDataPublisher
{
    Task PublishAsync(SensorData sensorData, CancellationToken cancellationToken = default);
    Task PublishAnomalyAsync(AnomalyDetectionResult anomaly, CancellationToken cancellationToken = default);
}
