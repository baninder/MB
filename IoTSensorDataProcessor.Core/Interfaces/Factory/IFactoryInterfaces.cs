using IoTSensorDataProcessor.Core.Models;
using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using IoTSensorDataProcessor.Core.Interfaces.Messaging;
using System.Threading.Channels;

namespace IoTSensorDataProcessor.Core.Interfaces.Factory;

/// <summary>
/// Abstract factory pattern for creating strategy instances
/// </summary>
public interface IStrategyFactory<TStrategy> where TStrategy : class
{
    TStrategy CreateStrategy(string strategyName);
    TStrategy CreateDefaultStrategy();
    IEnumerable<TStrategy> GetAllStrategies();
    bool TryCreateStrategy(string strategyName, out TStrategy? strategy);
}

/// <summary>
/// Factory for anomaly detection strategies
/// </summary>
public interface IAnomalyDetectionStrategyFactory : IStrategyFactory<IAnomalyDetectionStrategy>
{
    IAnomalyDetectionStrategy CreateStrategyForSensorType(string sensorType);
    IEnumerable<IAnomalyDetectionStrategy> GetCompatibleStrategies(string sensorType);
}

/// <summary>
/// Factory for processing strategies
/// </summary>
public interface IProcessingStrategyFactory<TInput, TOutput> : IStrategyFactory<IProcessingStrategy<TInput, TOutput>>
{
    IProcessingStrategy<TInput, TOutput> CreateStrategyForInput(TInput input);
    IEnumerable<IProcessingStrategy<TInput, TOutput>> GetOrderedStrategies();
}

/// <summary>
/// Factory for serialization strategies
/// </summary>
public interface ISerializationStrategyFactory : IStrategyFactory<ISerializationStrategy<object>>
{
    ISerializationStrategy<T> CreateStrategy<T>(string contentType);
    ISerializationStrategy<T> CreateStrategyForType<T>();
    bool SupportsContentType(string contentType);
}

/// <summary>
/// Factory for publishing strategies
/// </summary>
public interface IPublishingStrategyFactory : IStrategyFactory<IPublishingStrategy<object>>
{
    IPublishingStrategy<T> CreateStrategy<T>(string strategyName);
    IPublishingStrategy<T> CreateStrategyForMessageType<T>();
    IEnumerable<IPublishingStrategy<T>> GetCompatibleStrategies<T>();
}

/// <summary>
/// Abstract factory for creating domain services
/// </summary>
public interface IServiceFactory
{
    TService CreateService<TService>() where TService : class;
    TService CreateService<TService>(params object[] args) where TService : class;
    bool TryCreateService<TService>(out TService? service) where TService : class;
}

/// <summary>
/// Factory for creating channel instances
/// </summary>
/// <typeparam name="T">Channel message type</typeparam>
public interface IChannelFactory<T>
{
    Task<Channel<T>> CreateChannelAsync(string channelName, ChannelOptions? options = null);
    Task<Channel<T>> CreateBoundedChannelAsync(string channelName, int capacity, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait);
    Task<Channel<T>> CreateUnboundedChannelAsync(string channelName);
    void DisposeChannel(string channelName);
}

/// <summary>
/// Factory for creating message brokers
/// </summary>
public interface IMessageBrokerFactory
{
    IMessageBroker<T> CreateBroker<T>(string brokerType, IDictionary<string, object> configuration);
    IMessageBroker<T> CreateSparkBroker<T>(string sparkConfig);
    IMessageBroker<T> CreateMqttBroker<T>(string mqttConfig);
    bool SupportsBrokerType(string brokerType);
}
