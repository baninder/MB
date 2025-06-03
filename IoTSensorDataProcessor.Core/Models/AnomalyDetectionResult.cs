namespace IoTSensorDataProcessor.Core.Models;

public class AnomalyDetectionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DeviceId { get; set; } = string.Empty;
    public string SensorType { get; set; } = string.Empty;
    public AnomalyType Type { get; set; }
    public double Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public SensorData OriginalData { get; set; } = default!;
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

public enum AnomalyType
{
    OutOfRange,
    SuddenSpike,
    SuddenDrop,
    FlatLine,
    NetworkTimeout,
    DataCorruption
}
