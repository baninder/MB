using IoTSensorDataProcessor.Core.Interfaces.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Strategy.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Factory;

/// <summary>
/// Factory for creating serialization strategies
/// </summary>
public class SerializationStrategyFactory : ISerializationStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SerializationStrategyFactory> _logger;
    private readonly Dictionary<string, Type> _strategies;

    public SerializationStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<SerializationStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Register available strategies
        _strategies = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/json"] = typeof(JsonSerializationStrategy<>),
            ["json"] = typeof(JsonSerializationStrategy<>),
            ["application/xml"] = typeof(XmlSerializationStrategy<>),
            ["xml"] = typeof(XmlSerializationStrategy<>),
            ["text/xml"] = typeof(XmlSerializationStrategy<>)
        };
    }

    public ISerializationStrategy<object> CreateStrategy(string strategyName)
    {
        return CreateStrategy<object>(strategyName);
    }

    public ISerializationStrategy<T> CreateStrategy<T>(string contentType)
    {
        try
        {
            if (!_strategies.TryGetValue(contentType, out var strategyType))
            {
                _logger.LogWarning("Unknown serialization content type: {ContentType}. Using JSON.", contentType);
                return CreateStrategyForType<T>();
            }

            // Make generic type
            var genericType = strategyType.MakeGenericType(typeof(T));
            var strategy = (ISerializationStrategy<T>)_serviceProvider.GetRequiredService(genericType);
            
            _logger.LogDebug("Created serialization strategy for {ContentType}: {StrategyType}", contentType, genericType.Name);
            
            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating serialization strategy for content type: {ContentType}", contentType);
            throw;
        }
    }

    public ISerializationStrategy<T> CreateStrategyForType<T>()
    {
        // Default to JSON serialization
        return _serviceProvider.GetRequiredService<JsonSerializationStrategy<T>>();
    }

    public bool SupportsContentType(string contentType)
    {
        return _strategies.ContainsKey(contentType);
    }

    public ISerializationStrategy<object> CreateDefaultStrategy()
    {
        return CreateStrategyForType<object>();
    }

    public IEnumerable<ISerializationStrategy<object>> GetAllStrategies()
    {
        var strategies = new List<ISerializationStrategy<object>>();
        
        foreach (var strategyType in _strategies.Values.Distinct())
        {
            try
            {
                var genericType = strategyType.MakeGenericType(typeof(object));
                var strategy = (ISerializationStrategy<object>)_serviceProvider.GetRequiredService(genericType);
                strategies.Add(strategy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create serialization strategy of type {StrategyType}", strategyType.Name);
            }
        }
        
        return strategies;
    }

    public bool TryCreateStrategy(string strategyName, out ISerializationStrategy<object>? strategy)
    {
        try
        {
            strategy = CreateStrategy(strategyName);
            return true;
        }
        catch
        {
            strategy = null;
            return false;
        }
    }
}
