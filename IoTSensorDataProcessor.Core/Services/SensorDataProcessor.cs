using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using System.Threading.Channels;

namespace IoTSensorDataProcessor.Core.Services;

public class SensorDataProcessor : ISensorDataProcessor
{
    private readonly Channel<SensorData> _processedDataChannel;
    private readonly ChannelWriter<SensorData> _writer;
    private readonly ChannelReader<SensorData> _reader;
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly ISensorDataRepository _repository;
    private readonly ISensorDataPublisher _publisher;

    public SensorDataProcessor(
        IAnomalyDetectionService anomalyDetectionService,
        ISensorDataRepository repository,
        ISensorDataPublisher publisher)
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _processedDataChannel = Channel.CreateBounded<SensorData>(options);
        _writer = _processedDataChannel.Writer;
        _reader = _processedDataChannel.Reader;
        _anomalyDetectionService = anomalyDetectionService;
        _repository = repository;
        _publisher = publisher;
    }

    public async Task ProcessAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and enrich sensor data
            ValidateAndEnrichSensorData(sensorData);

            // Save to database
            await _repository.SaveAsync(sensorData, cancellationToken);

            // Detect anomalies
            var anomaly = await _anomalyDetectionService.DetectAnomalyAsync(sensorData, cancellationToken);
            if (anomaly != null)
            {
                await _publisher.PublishAnomalyAsync(anomaly, cancellationToken);
            }

            // Publish processed data
            await _publisher.PublishAsync(sensorData, cancellationToken);

            // Add to processed data channel for real-time consumers
            await _writer.WriteAsync(sensorData, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error and continue processing
            Console.WriteLine($"Error processing sensor data: {ex.Message}");
        }
    }

    public ChannelReader<SensorData> GetProcessedDataChannel() => _reader;    private static void ValidateAndEnrichSensorData(SensorData sensorData)
    {
        if (string.IsNullOrEmpty(sensorData.DeviceId))
            throw new ArgumentException("DeviceId is required");

        if (string.IsNullOrEmpty(sensorData.SensorType))
            throw new ArgumentException("SensorType is required");

        // Note: Timestamp is set during SensorData creation via factory methods
        // and cannot be modified due to private setter (immutable design)
        
        // Add processing timestamp
        sensorData.Metadata["ProcessedAt"] = DateTime.UtcNow;
    }
}
