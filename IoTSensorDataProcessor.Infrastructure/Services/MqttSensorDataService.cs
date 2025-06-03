using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace IoTSensorDataProcessor.Infrastructure.Services;

public class MqttSensorDataService : IMqttSensorDataService, IDisposable
{
    private readonly IManagedMqttClient _mqttClient;
    private readonly Channel<SensorData> _dataChannel;
    private readonly ChannelWriter<SensorData> _writer;
    private readonly ChannelReader<SensorData> _reader;
    private readonly string _brokerHost;
    private readonly int _brokerPort;
    private readonly string _username;
    private readonly string _password;
    private readonly List<string> _topics;
    private bool _disposed;

    public event EventHandler<SensorData>? SensorDataReceived;

    public MqttSensorDataService(
        string brokerHost = "localhost",
        int brokerPort = 1883,
        string username = "",
        string password = "",
        List<string>? topics = null)
    {
        _brokerHost = brokerHost;
        _brokerPort = brokerPort;
        _username = username;
        _password = password;
        _topics = topics ?? new List<string> { "sensors/+/data", "iot/+/telemetry" };

        var options = new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _dataChannel = Channel.CreateBounded<SensorData>(options);
        _writer = _dataChannel.Writer;
        _reader = _dataChannel.Reader;

        var factory = new MqttFactory();
        _mqttClient = factory.CreateManagedMqttClient();
        
        ConfigureMqttClient();
    }

    private void ConfigureMqttClient()
    {
        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
        _mqttClient.ConnectedAsync += OnConnected;
        _mqttClient.DisconnectedAsync += OnDisconnected;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var clientOptionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_brokerHost, _brokerPort)
            .WithCleanSession();

        if (!string.IsNullOrEmpty(_username))
        {
            clientOptionsBuilder.WithCredentials(_username, _password);
        }

        var managedOptions = new ManagedMqttClientOptionsBuilder()
            .WithClientOptions(clientOptionsBuilder.Build())
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .Build();

        await _mqttClient.StartAsync(managedOptions);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _mqttClient.StopAsync();
        _writer.Complete();
    }

    public ChannelReader<SensorData> GetDataChannel() => _reader;

    private async Task OnConnected(MqttClientConnectedEventArgs args)
    {
        Console.WriteLine("MQTT Client connected to broker");

        // Subscribe to sensor data topics
        var subscriptions = _topics.Select(topic => 
            new MqttTopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build()).ToArray();

        await _mqttClient.SubscribeAsync(subscriptions);
        Console.WriteLine($"Subscribed to {subscriptions.Length} MQTT topics");
    }

    private Task OnDisconnected(MqttClientDisconnectedEventArgs args)
    {
        Console.WriteLine($"MQTT Client disconnected: {args.Reason}");
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment);
            var topic = args.ApplicationMessage.Topic;

            Console.WriteLine($"Received MQTT message from topic: {topic}");

            var sensorData = ParseSensorData(payload, topic);
            if (sensorData != null)
            {
                await _writer.WriteAsync(sensorData);
                SensorDataReceived?.Invoke(this, sensorData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing MQTT message: {ex.Message}");
        }
    }

    private static SensorData? ParseSensorData(string payload, string topic)
    {
        try
        {
            // Try to parse as JSON first
            if (payload.TrimStart().StartsWith('{'))
            {
                var jsonData = JsonSerializer.Deserialize<SensorData>(payload);
                if (jsonData != null)
                {                    // Extract device ID from topic if not present in payload
                    if (string.IsNullOrEmpty(jsonData.DeviceId))
                    {
                        // Since SensorData is immutable, we need to recreate it with proper DeviceId
                        var deviceId = ExtractDeviceIdFromTopic(topic);                        return SensorData.Create(
                            deviceId,
                            jsonData.SensorType,
                            jsonData.Value.Value,
                            jsonData.Value.Unit
                        );
                    }
                    return jsonData;
                }
            }

            // Parse simple key-value format: "temperature:25.5,humidity:60.2"
            return ParseSimpleFormat(payload, topic);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing sensor data: {ex.Message}");
            return null;
        }
    }    private static SensorData ParseSimpleFormat(string payload, string topic)
    {
        var deviceId = ExtractDeviceIdFromTopic(topic);
        var parts = payload.Split(',');
        
        if (parts.Length == 0) 
        {
            // Return default sensor data with minimal valid values
            return SensorData.Create(deviceId, "unknown", 0.0, "unknown");
        }

        var firstValue = parts[0].Split(':');
        if (firstValue.Length != 2) 
        {
            return SensorData.Create(deviceId, "unknown", 0.0, "unknown");
        }

        var sensorType = firstValue[0].Trim();
        var value = double.TryParse(firstValue[1].Trim(), out var parsedValue) ? parsedValue : 0.0;
        
        // Create the sensor data using factory method
        var sensorData = SensorData.Create(deviceId, sensorType, value, "unit");

        // Add additional values to metadata
        for (int i = 1; i < parts.Length; i++)
        {
            var kvp = parts[i].Split(':');
            if (kvp.Length == 2)
            {
                sensorData.AddMetadata(kvp[0].Trim(), kvp[1].Trim());
            }
        }

        return sensorData;
    }

    private static string ExtractDeviceIdFromTopic(string topic)
    {
        // Extract device ID from topic patterns like "sensors/device123/data" or "iot/device456/telemetry"
        var parts = topic.Split('/');        return parts.Length > 1 ? parts[1] : "unknown";
    }    public async Task PublishSensorDataAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        if (!_mqttClient.IsConnected)
            return;

        var topic = $"sensors/{sensorData.DeviceId}/data";
        var payload = System.Text.Json.JsonSerializer.Serialize(sensorData);
        
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.EnqueueAsync(message);
    }    public async Task PublishAnomalyAsync(AnomalyDetectionResult anomaly, CancellationToken cancellationToken = default)
    {
        if (!_mqttClient.IsConnected)
            return;

        var topic = $"alerts/{anomaly.DeviceId}/anomaly";
        var payload = System.Text.Json.JsonSerializer.Serialize(anomaly);
        
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _mqttClient.EnqueueAsync(message);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _mqttClient?.Dispose();
            _writer.Complete();
            _disposed = true;
        }
    }
}
