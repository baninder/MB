using IoTSensorDataProcessor.Core.Interfaces.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Strategy.AnomalyDetection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Factory;

/// <summary>
/// Factory for creating anomaly detection strategies
/// </summary>
public class AnomalyDetectionStrategyFactory : IAnomalyDetectionStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnomalyDetectionStrategyFactory> _logger;
    private readonly Dictionary<string, Type> _strategies;

    public AnomalyDetectionStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<AnomalyDetectionStrategyFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Register available strategies
        _strategies = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            ["statistical"] = typeof(StatisticalAnomalyDetectionStrategy),
            ["timeseries"] = typeof(TimeSeriesAnomalyDetectionStrategy),
            ["threshold"] = typeof(ThresholdAnomalyDetectionStrategy)
        };
    }

    public IAnomalyDetectionStrategy CreateStrategy(string strategyName)
    {
        try
        {
            if (!_strategies.TryGetValue(strategyName, out var strategyType))
            {
                _logger.LogWarning("Unknown anomaly detection strategy: {StrategyName}. Using default.", strategyName);
                return CreateDefaultStrategy();
            }

            var strategy = (IAnomalyDetectionStrategy)_serviceProvider.GetRequiredService(strategyType);
            _logger.LogDebug("Created anomaly detection strategy: {StrategyName}", strategyName);
            
            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating anomaly detection strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    public IAnomalyDetectionStrategy CreateDefaultStrategy()
    {
        return _serviceProvider.GetRequiredService<StatisticalAnomalyDetectionStrategy>();
    }

    public IEnumerable<IAnomalyDetectionStrategy> GetAllStrategies()
    {
        var strategies = new List<IAnomalyDetectionStrategy>();
        
        foreach (var strategyType in _strategies.Values)
        {
            try
            {
                var strategy = (IAnomalyDetectionStrategy)_serviceProvider.GetRequiredService(strategyType);
                strategies.Add(strategy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create strategy of type {StrategyType}", strategyType.Name);
            }
        }
        
        return strategies;
    }

    public bool TryCreateStrategy(string strategyName, out IAnomalyDetectionStrategy? strategy)
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

    public IAnomalyDetectionStrategy CreateStrategyForSensorType(string sensorType)
    {
        // Select best strategy based on sensor type
        var strategyName = sensorType.ToLowerInvariant() switch
        {
            var type when type.Contains("temperature") || 
                         type.Contains("humidity") || 
                         type.Contains("pressure") => "timeseries",
            var type when type.Contains("switch") || 
                         type.Contains("motion") || 
                         type.Contains("door") => "threshold",
            _ => "statistical"
        };

        return CreateStrategy(strategyName);
    }

    public IEnumerable<IAnomalyDetectionStrategy> GetCompatibleStrategies(string sensorType)
    {
        return GetAllStrategies().Where(s => s.CanHandle(sensorType));
    }
}
