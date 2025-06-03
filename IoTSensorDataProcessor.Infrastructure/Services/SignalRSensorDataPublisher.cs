using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace IoTSensorDataProcessor.Infrastructure.Services;

public class SignalRSensorDataPublisher : ISensorDataPublisher
{
    private readonly IHubContext<SensorDataHub> _hubContext;

    public SignalRSensorDataPublisher(IHubContext<SensorDataHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("SensorDataReceived", sensorData, cancellationToken);
        await _hubContext.Clients.Group($"device_{sensorData.DeviceId}").SendAsync("DeviceDataReceived", sensorData, cancellationToken);
        await _hubContext.Clients.Group($"sensor_{sensorData.SensorType}").SendAsync("SensorTypeDataReceived", sensorData, cancellationToken);
    }

    public async Task PublishAnomalyAsync(AnomalyDetectionResult anomaly, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("AnomalyDetected", anomaly, cancellationToken);
        await _hubContext.Clients.Group($"device_{anomaly.DeviceId}").SendAsync("DeviceAnomalyDetected", anomaly, cancellationToken);
        await _hubContext.Clients.Group("alerts").SendAsync("AlertReceived", anomaly, cancellationToken);
    }
}

public class SensorDataHub : Hub
{
    public async Task JoinDeviceGroup(string deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }

    public async Task LeaveDeviceGroup(string deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
    }

    public async Task JoinSensorTypeGroup(string sensorType)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sensor_{sensorType}");
    }

    public async Task LeaveSensorTypeGroup(string sensorType)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sensor_{sensorType}");
    }

    public async Task JoinAlertsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "alerts");
    }

    public async Task LeaveAlertsGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "alerts");
    }
}
