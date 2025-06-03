namespace IoTSensorDataProcessor.Core.Interfaces.Messaging;

/// <summary>
/// Generic message broker interface with Spark integration
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface IMessageBroker<T> : IDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task PublishAsync(T message, string topic, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<T> messages, string topic, CancellationToken cancellationToken = default);
    Task SubscribeAsync(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default);
    Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    string BrokerType { get; }
}

/// <summary>
/// Spark-specific message broker interface
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface ISparkMessageBroker<T> : IMessageBroker<T>
{
    Task<IDataFrame<T>> CreateDataFrameAsync(IEnumerable<T> data, CancellationToken cancellationToken = default);
    Task<IDataFrame<T>> ReadFromSourceAsync(string source, CancellationToken cancellationToken = default);
    Task WriteToSinkAsync(IDataFrame<T> dataFrame, string sink, CancellationToken cancellationToken = default);
    Task<ISparkSession> GetSparkSessionAsync();
    Task ExecuteSparkJobAsync(string jobName, Func<ISparkSession, Task> job, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteSparkJobAsync<TResult>(string jobName, Func<ISparkSession, Task<TResult>> job, CancellationToken cancellationToken = default);
    Task PublishToKafkaAsync(T message, string topic, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> ReadFromKafkaAsync(string topic, CancellationToken cancellationToken = default);
}

/// <summary>
/// Spark session wrapper interface
/// </summary>
public interface ISparkSession : IDisposable
{
    string ApplicationId { get; }
    string ApplicationName { get; }
    Task<IDataFrame<T>> CreateDataFrameAsync<T>(IEnumerable<T> data);
    Task<IDataFrame<T>> ReadFromSourceAsync<T>(string source, IDictionary<string, string> options);
    Task WriteToSinkAsync<T>(IDataFrame<T> dataFrame, string sink, IDictionary<string, string> options);
    Task ExecuteSqlAsync(string sql);
    Task<IDataFrame<T>> SqlAsync<T>(string sql);
}

/// <summary>
/// DataFrame interface for Spark operations
/// </summary>
/// <typeparam name="T">Row type</typeparam>
public interface IDataFrame<T>
{
    Task<IEnumerable<T>> CollectAsync();
    Task<long> CountAsync();
    IDataFrame<T> Filter(Func<T, bool> predicate);
    IDataFrame<TResult> Select<TResult>(Func<T, TResult> selector);
    IDataFrame<T> OrderBy<TKey>(Func<T, TKey> keySelector);
    Task WriteToSinkAsync(string format, IDictionary<string, string> options);
    Task ShowAsync(int numRows = 20);
}

/// <summary>
/// Event-driven message broker interface
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public interface IEventBroker<T>
{
    event EventHandler<MessageReceivedEventArgs<T>>? MessageReceived;
    event EventHandler<MessagePublishedEventArgs<T>>? MessagePublished;
    event EventHandler<BrokerErrorEventArgs>? ErrorOccurred;
    event EventHandler<BrokerStatusEventArgs>? StatusChanged;
}

/// <summary>
/// Message received event arguments
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public class MessageReceivedEventArgs<T> : EventArgs
{
    public T Message { get; set; } = default!;
    public string Topic { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Message published event arguments
/// </summary>
/// <typeparam name="T">Message type</typeparam>
public class MessagePublishedEventArgs<T> : EventArgs
{
    public T Message { get; set; } = default!;
    public string Topic { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Broker error event arguments
/// </summary>
public class BrokerErrorEventArgs : EventArgs
{
    public Exception Exception { get; set; } = default!;
    public string Operation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IDictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Broker status event arguments
/// </summary>
public class BrokerStatusEventArgs : EventArgs
{
    public BrokerStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Broker status enumeration
/// </summary>
public enum BrokerStatus
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Error,
    Stopping,
    Stopped
}
