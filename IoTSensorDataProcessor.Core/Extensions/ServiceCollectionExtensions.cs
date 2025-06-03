using IoTSensorDataProcessor.Core.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Factory;
using IoTSensorDataProcessor.Core.Interfaces.Messaging;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Messaging;
using IoTSensorDataProcessor.Core.Strategy.AnomalyDetection;
using IoTSensorDataProcessor.Core.Strategy.Processing;
using IoTSensorDataProcessor.Core.Strategy.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Spark.Sql;

namespace IoTSensorDataProcessor.Core.Extensions;

/// <summary>
/// Dependency injection extensions for advanced patterns
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register all advanced design patterns and strategies
    /// </summary>
    public static IServiceCollection AddAdvancedPatterns(this IServiceCollection services)
    {
        // Register Strategy Pattern implementations
        services.AddStrategies();
        
        // Register Factory Pattern implementations
        services.AddFactories();
        
        // Register Spark integration
        services.AddSparkIntegration();
        
        return services;
    }

    /// <summary>
    /// Register all strategy pattern implementations
    /// </summary>
    public static IServiceCollection AddStrategies(this IServiceCollection services)
    {
        // Anomaly Detection Strategies
        services.AddTransient<StatisticalAnomalyDetectionStrategy>();
        services.AddTransient<TimeSeriesAnomalyDetectionStrategy>();
        services.AddTransient<ThresholdAnomalyDetectionStrategy>();

        // Processing Strategies
        services.AddTransient<DataCleaningProcessingStrategy>();
        services.AddTransient<DataEnrichmentProcessingStrategy>();

        // Serialization Strategies
        services.AddTransient(typeof(JsonSerializationStrategy<>));
        services.AddTransient(typeof(XmlSerializationStrategy<>));

        return services;
    }

    /// <summary>
    /// Register all factory pattern implementations
    /// </summary>
    public static IServiceCollection AddFactories(this IServiceCollection services)
    {
        services.AddSingleton<IAnomalyDetectionStrategyFactory, AnomalyDetectionStrategyFactory>();
        services.AddSingleton<ISerializationStrategyFactory, SerializationStrategyFactory>();
        
        // Add processing strategy factory
        services.AddSingleton(typeof(IProcessingStrategyFactory<,>), typeof(ProcessingStrategyFactory<,>));

        return services;
    }

    /// <summary>
    /// Register Spark integration services
    /// </summary>
    public static IServiceCollection AddSparkIntegration(this IServiceCollection services)
    {
        // Register Spark session as singleton
        services.AddSingleton<SparkSession>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SparkSession>>();
            
            try
            {
                var spark = SparkSession
                    .Builder()
                    .AppName("IoTSensorDataProcessor")
                    .GetOrCreate();
                
                logger.LogInformation("Spark session created successfully");
                return spark;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create Spark session");
                throw;
            }
        });

        // Register Spark message broker
        services.AddScoped(typeof(ISparkMessageBroker<>), typeof(SparkMessageBroker<>));

        return services;
    }    /// <summary>
    /// Configure strategy-specific settings
    /// </summary>
    public static IServiceCollection ConfigureStrategies(this IServiceCollection services, Action<StrategyOptions> configure)
    {
        services.Configure<StrategyOptions>(configure);
        return services;
    }
}

/// <summary>
/// Configuration options for strategies
/// </summary>
public class StrategyOptions
{
    /// <summary>
    /// Default anomaly detection strategy name
    /// </summary>
    public string DefaultAnomalyStrategy { get; set; } = "statistical";

    /// <summary>
    /// Anomaly detection thresholds per strategy
    /// </summary>
    public Dictionary<string, double> AnomalyThresholds { get; set; } = new()
    {
        ["statistical"] = 2.5,
        ["timeseries"] = 0.3,
        ["threshold"] = 1.0
    };

    /// <summary>
    /// Processing strategy priorities
    /// </summary>
    public Dictionary<string, int> ProcessingPriorities { get; set; } = new()
    {
        ["DataCleaning"] = 1,
        ["DataEnrichment"] = 5
    };

    /// <summary>
    /// Default serialization format
    /// </summary>
    public string DefaultSerializationFormat { get; set; } = "application/json";

    /// <summary>
    /// Spark configuration
    /// </summary>
    public SparkConfiguration Spark { get; set; } = new();
}

/// <summary>
/// Spark-specific configuration
/// </summary>
public class SparkConfiguration
{
    /// <summary>
    /// Spark application name
    /// </summary>
    public string AppName { get; set; } = "IoTSensorDataProcessor";

    /// <summary>
    /// Spark master URL
    /// </summary>
    public string? Master { get; set; }

    /// <summary>
    /// Additional Spark configuration properties
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>
    /// Enable Spark SQL
    /// </summary>
    public bool EnableSql { get; set; } = true;

    /// <summary>
    /// Default parallelism level
    /// </summary>
    public int DefaultParallelism { get; set; } = Environment.ProcessorCount;
}
