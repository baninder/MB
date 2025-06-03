using IoTSensorDataProcessor.Core.Models;

namespace IoTSensorDataProcessor.Core.Interfaces.Strategy;

/// <summary>
/// Strategy pattern for anomaly detection algorithms
/// </summary>
public interface IAnomalyDetectionStrategy
{
    string StrategyName { get; }
    Task<AnomalyDetectionResult?> DetectAsync(SensorData sensorData, IEnumerable<SensorData> historicalData, CancellationToken cancellationToken = default);
    bool CanHandle(string sensorType);
    double Threshold { get; set; }
}

/// <summary>
/// Strategy pattern for data processing pipelines
/// </summary>
/// <typeparam name="TInput">Input data type</typeparam>
/// <typeparam name="TOutput">Output data type</typeparam>
public interface IProcessingStrategy<TInput, TOutput>
{
    string StrategyName { get; }
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);
    bool CanProcess(TInput input);
    int Priority { get; }
}

/// <summary>
/// Strategy pattern for data serialization
/// </summary>
/// <typeparam name="T">Data type to serialize</typeparam>
public interface ISerializationStrategy<T>
{
    string ContentType { get; }
    Task<string> SerializeAsync(T data, CancellationToken cancellationToken = default);
    Task<T?> DeserializeAsync(string data, CancellationToken cancellationToken = default);
    bool SupportsType(Type type);
}

/// <summary>
/// Strategy pattern for message publishing
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface IPublishingStrategy<T>
{
    string StrategyName { get; }
    Task<bool> PublishAsync(T message, string topic, CancellationToken cancellationToken = default);
    Task<bool> PublishBatchAsync(IEnumerable<T> messages, string topic, CancellationToken cancellationToken = default);
    bool SupportsMessageType(Type messageType);
    int MaxBatchSize { get; }
}

/// <summary>
/// Strategy pattern for data validation
/// </summary>
/// <typeparam name="T">Data type to validate</typeparam>
public interface IValidationStrategy<T>
{
    string ValidatorName { get; }
    Task<ValidationResult> ValidateAsync(T data, CancellationToken cancellationToken = default);
    bool CanValidate(Type dataType);
    int Priority { get; }
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
