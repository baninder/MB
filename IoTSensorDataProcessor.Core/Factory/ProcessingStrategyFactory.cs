using IoTSensorDataProcessor.Core.Interfaces.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Strategy.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTSensorDataProcessor.Core.Factory;

/// <summary>
/// Factory for creating processing strategies
/// </summary>
public class ProcessingStrategyFactory<TInput, TOutput> : IProcessingStrategyFactory<TInput, TOutput>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessingStrategyFactory<TInput, TOutput>> _logger;
    private readonly Dictionary<string, Type> _strategies;

    public ProcessingStrategyFactory(
        IServiceProvider serviceProvider,
        ILogger<ProcessingStrategyFactory<TInput, TOutput>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Register available strategies (assuming TInput and TOutput are SensorData for core strategies)
        _strategies = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        // Only register if input/output types match our concrete implementations
        if (typeof(TInput) == typeof(Models.SensorData) && typeof(TOutput) == typeof(Models.SensorData))
        {
            _strategies["datacleaning"] = typeof(DataCleaningProcessingStrategy);
            _strategies["dataenrichment"] = typeof(DataEnrichmentProcessingStrategy);
        }
    }

    public IProcessingStrategy<TInput, TOutput> CreateStrategy(string strategyName)
    {
        try
        {
            if (!_strategies.TryGetValue(strategyName, out var strategyType))
            {
                _logger.LogWarning("Unknown processing strategy: {StrategyName}. Using default.", strategyName);
                return CreateDefaultStrategy();
            }

            var strategy = (IProcessingStrategy<TInput, TOutput>)_serviceProvider.GetRequiredService(strategyType);
            _logger.LogDebug("Created processing strategy: {StrategyName}", strategyName);
            
            return strategy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating processing strategy: {StrategyName}", strategyName);
            throw;
        }
    }

    public IProcessingStrategy<TInput, TOutput> CreateDefaultStrategy()
    {
        if (_strategies.Any())
        {
            var defaultStrategyType = _strategies.Values.First();
            return (IProcessingStrategy<TInput, TOutput>)_serviceProvider.GetRequiredService(defaultStrategyType);
        }

        throw new InvalidOperationException($"No processing strategies available for types {typeof(TInput).Name} -> {typeof(TOutput).Name}");
    }

    public IEnumerable<IProcessingStrategy<TInput, TOutput>> GetAllStrategies()
    {
        var strategies = new List<IProcessingStrategy<TInput, TOutput>>();
        
        foreach (var strategyType in _strategies.Values)
        {
            try
            {
                var strategy = (IProcessingStrategy<TInput, TOutput>)_serviceProvider.GetRequiredService(strategyType);
                strategies.Add(strategy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create strategy of type {StrategyType}", strategyType.Name);
            }
        }
        
        return strategies;
    }

    public bool TryCreateStrategy(string strategyName, out IProcessingStrategy<TInput, TOutput>? strategy)
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

    public IProcessingStrategy<TInput, TOutput> CreateStrategyForInput(TInput input)
    {
        // For sensor data, we can select strategy based on data characteristics
        if (input is Models.SensorData sensorData)
        {
            // Always start with data cleaning
            return CreateStrategy("datacleaning");
        }

        return CreateDefaultStrategy();
    }

    public IEnumerable<IProcessingStrategy<TInput, TOutput>> GetOrderedStrategies()
    {
        return GetAllStrategies().OrderBy(s => s.Priority);
    }
}
