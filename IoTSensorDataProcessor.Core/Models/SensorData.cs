using System.Text.Json.Serialization;
using IoTSensorDataProcessor.Core.Models.Common;

namespace IoTSensorDataProcessor.Core.Models;

/// <summary>
/// Sensor data aggregate root with domain events
/// </summary>
public class SensorData : AggregateRoot<string>
{
    public string DeviceId { get; private set; } = string.Empty;
    public string SensorType { get; private set; } = string.Empty;
    public SensorValue Value { get; private set; } = default!;
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
    public Location? Location { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public SensorQuality Quality { get; private set; } = SensorQuality.Good;
    public SensorStatus Status { get; private set; } = SensorStatus.Active;

    // Constructor for deserialization
    private SensorData() : base()
    {
        Id = Guid.NewGuid().ToString();
    }

    // Factory method for creating sensor data
    public static SensorData Create(string deviceId, string sensorType, double value, string unit)
    {
        var sensorData = new SensorData
        {
            Id = Guid.NewGuid().ToString(),
            DeviceId = deviceId,
            SensorType = sensorType,
            Value = new SensorValue(value, unit),
            Timestamp = DateTime.UtcNow
        };

        sensorData.AddDomainEvent(new SensorDataCreatedEvent(sensorData));
        return sensorData;
    }

    public void UpdateValue(double value, string unit)
    {
        var oldValue = Value;
        Value = new SensorValue(value, unit);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new SensorValueUpdatedEvent(this, oldValue, Value));
    }

    public void UpdateQuality(SensorQuality quality)
    {
        if (Quality != quality)
        {
            var oldQuality = Quality;
            Quality = quality;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new SensorQualityChangedEvent(this, oldQuality, quality));
        }
    }

    public void UpdateLocation(double latitude, double longitude, double? altitude = null)
    {
        Location = new Location(latitude, longitude, altitude);
        UpdatedAt = DateTime.UtcNow;
        
        AddDomainEvent(new SensorLocationUpdatedEvent(this));
    }

    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(SensorStatus status)
    {
        if (Status != status)
        {
            var oldStatus = Status;
            Status = status;
            UpdatedAt = DateTime.UtcNow;
            
            AddDomainEvent(new SensorStatusChangedEvent(this, oldStatus, status));
        }
    }
}

/// <summary>
/// Sensor value value object
/// </summary>
public class SensorValue : ValueObject
{
    public double Value { get; }
    public string Unit { get; }

    public SensorValue(double value, string unit)
    {
        Value = value;
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Unit;
    }

    public override string ToString() => $"{Value} {Unit}";
}

/// <summary>
/// Location value object
/// </summary>
public class Location : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    public double? Altitude { get; }

    public Location(double latitude, double longitude, double? altitude = null)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90 degrees");
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180 degrees");

        Latitude = latitude;
        Longitude = longitude;
        Altitude = altitude;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Altitude;
    }

    public override string ToString() => $"{Latitude}, {Longitude}" + (Altitude.HasValue ? $" ({Altitude}m)" : "");
}

/// <summary>
/// Sensor quality enumeration
/// </summary>
public enum SensorQuality
{
    Unknown = 0,
    Good = 1,
    Warning = 2,
    Critical = 3,
    Offline = 4
}

/// <summary>
/// Sensor status enumeration
/// </summary>
public enum SensorStatus
{
    Unknown = 0,
    Active = 1,
    Inactive = 2,
    Maintenance = 3,
    Error = 4,
    Calibrating = 5
}

// Domain Events
public class SensorDataCreatedEvent : DomainEvent
{
    public SensorData SensorData { get; }

    public SensorDataCreatedEvent(SensorData sensorData)
    {
        SensorData = sensorData;
    }
}

public class SensorValueUpdatedEvent : DomainEvent
{
    public SensorData SensorData { get; }
    public SensorValue OldValue { get; }
    public SensorValue NewValue { get; }

    public SensorValueUpdatedEvent(SensorData sensorData, SensorValue oldValue, SensorValue newValue)
    {
        SensorData = sensorData;
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public class SensorQualityChangedEvent : DomainEvent
{
    public SensorData SensorData { get; }
    public SensorQuality OldQuality { get; }
    public SensorQuality NewQuality { get; }

    public SensorQualityChangedEvent(SensorData sensorData, SensorQuality oldQuality, SensorQuality newQuality)
    {
        SensorData = sensorData;
        OldQuality = oldQuality;
        NewQuality = newQuality;
    }
}

public class SensorLocationUpdatedEvent : DomainEvent
{
    public SensorData SensorData { get; }

    public SensorLocationUpdatedEvent(SensorData sensorData)
    {
        SensorData = sensorData;
    }
}

public class SensorStatusChangedEvent : DomainEvent
{
    public SensorData SensorData { get; }
    public SensorStatus OldStatus { get; }
    public SensorStatus NewStatus { get; }

    public SensorStatusChangedEvent(SensorData sensorData, SensorStatus oldStatus, SensorStatus newStatus)
    {
        SensorData = sensorData;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}
