using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IoTSensorDataProcessor.Core.Strategy.Serialization;

/// <summary>
/// JSON serialization strategy using System.Text.Json
/// </summary>
public class JsonSerializationStrategy<T> : ISerializationStrategy<T>
{
    private readonly ILogger<JsonSerializationStrategy<T>> _logger;
    private readonly JsonSerializerOptions _options;

    public string ContentType => "application/json";

    public JsonSerializationStrategy(ILogger<JsonSerializationStrategy<T>> logger)
    {
        _logger = logger;
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }    public async Task<string> SerializeAsync(T data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null)
                return "null";

            // Use Task.Run for CPU-bound serialization work
            var json = await Task.Run(() => JsonSerializer.Serialize(data, _options), cancellationToken);
            _logger.LogDebug("Serialized {Type} to JSON: {Length} characters", typeof(T).Name, json.Length);
            
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing {Type} to JSON", typeof(T).Name);
            throw;
        }
    }    public async Task<T?> DeserializeAsync(string data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(data) || data == "null")
                return default;

            // Use Task.Run for CPU-bound deserialization work
            var result = await Task.Run(() => JsonSerializer.Deserialize<T>(data, _options), cancellationToken);
            _logger.LogDebug("Deserialized JSON to {Type}", typeof(T).Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing JSON to {Type}: {Data}", typeof(T).Name, data);
            throw;
        }
    }

    public bool SupportsType(Type type)
    {
        // JSON serialization supports most types
        return !type.IsPointer && 
               type != typeof(IntPtr) && 
               type != typeof(UIntPtr);
    }
}
