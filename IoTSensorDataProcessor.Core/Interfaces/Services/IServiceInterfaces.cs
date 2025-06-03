using IoTSensorDataProcessor.Core.Models;

namespace IoTSensorDataProcessor.Core.Interfaces.Services;

/// <summary>
/// Command pattern interface
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public interface ICommand<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    bool CanExecute(TRequest request);
    string CommandName { get; }
}

/// <summary>
/// Query pattern interface
/// </summary>
/// <typeparam name="TQuery">Query type</typeparam>
/// <typeparam name="TResult">Result type</typeparam>
public interface IQuery<TQuery, TResult>
{
    Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default);
    bool CanExecute(TQuery query);
    string QueryName { get; }
}

/// <summary>
/// CQRS Command handler
/// </summary>
/// <typeparam name="TCommand">Command type</typeparam>
/// <typeparam name="TResult">Result type</typeparam>
public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// CQRS Query handler
/// </summary>
/// <typeparam name="TQuery">Query type</typeparam>
/// <typeparam name="TResult">Result type</typeparam>
public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>
/// Mediator pattern for CQRS
/// </summary>
public interface IMediator
{
    Task<TResult> SendAsync<TResult>(ICommand<object, TResult> command, CancellationToken cancellationToken = default);
    Task<TResult> QueryAsync<TResult>(IQuery<object, TResult> query, CancellationToken cancellationToken = default);
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : class;
}

/// <summary>
/// Generic cache interface
/// </summary>
/// <typeparam name="TKey">Cache key type</typeparam>
/// <typeparam name="TValue">Cache value type</typeparam>
public interface ICache<TKey, TValue>
{
    Task<TValue?> GetAsync(TKey key, CancellationToken cancellationToken = default);
    Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey key, CancellationToken cancellationToken = default);
    Task RemoveAsync(TKey key, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TKey>> GetKeysAsync(string pattern = "*", CancellationToken cancellationToken = default);
}

/// <summary>
/// Distributed cache interface
/// </summary>
/// <typeparam name="TKey">Cache key type</typeparam>
/// <typeparam name="TValue">Cache value type</typeparam>
public interface IDistributedCache<TKey, TValue> : ICache<TKey, TValue>
{
    Task<TValue?> GetOrSetAsync(TKey key, Func<Task<TValue>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    Task InvalidatePatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(TKey key, long value = 1, CancellationToken cancellationToken = default);
    Task<double> IncrementAsync(TKey key, double value, CancellationToken cancellationToken = default);
}

/// <summary>
/// Health check interface
/// </summary>
public interface IHealthCheck
{
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
    string Name { get; }
    TimeSpan Timeout { get; }
}

/// <summary>
/// Health check result
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Health status enumeration
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Resilience policy interface
/// </summary>
/// <typeparam name="T">Return type</typeparam>
public interface IResiliencePolicy<T>
{
    Task<T> ExecuteAsync(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    string PolicyName { get; }
}

/// <summary>
/// Circuit breaker interface
/// </summary>
public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    CircuitBreakerState State { get; }
    event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// Circuit breaker state
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

/// <summary>
/// Circuit breaker state changed event arguments
/// </summary>
public class CircuitBreakerStateChangedEventArgs : EventArgs
{
    public CircuitBreakerState PreviousState { get; set; }
    public CircuitBreakerState CurrentState { get; set; }
    public DateTime Timestamp { get; set; }
    public Exception? LastException { get; set; }
}
