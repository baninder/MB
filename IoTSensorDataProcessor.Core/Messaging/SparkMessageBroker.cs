using IoTSensorDataProcessor.Core.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Spark.Sql;
using Microsoft.Spark.Sql.Types;
using System.Text.Json;

namespace IoTSensorDataProcessor.Core.Messaging;

/// <summary>
/// Apache Spark-based message broker implementation
/// </summary>
public class SparkMessageBroker<T> : ISparkMessageBroker<T>
{
    private readonly SparkSession _spark;
    private readonly ILogger<SparkMessageBroker<T>> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _isConnected = true;
    private bool _disposed = false;

    public bool IsConnected => _isConnected;
    public string BrokerType => "Spark";

    public SparkMessageBroker(SparkSession spark, ILogger<SparkMessageBroker<T>> logger)
    {
        _spark = spark;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // IMessageBroker interface methods
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _isConnected = true;
            _logger.LogInformation("Spark message broker started");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Spark message broker");
            _isConnected = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _isConnected = false;
            _logger.LogInformation("Spark message broker stopped");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Spark message broker");
            throw;
        }
    }

    public async Task PublishAsync(T message, string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new[] { message };
            await PublishBatchAsync(messages, topic, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing single message to topic {Topic}", topic);
            throw;
        }
    }

    public async Task PublishBatchAsync(IEnumerable<T> messages, string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageList = messages.ToList();
            if (!messageList.Any())
            {
                return;
            }

            // Convert messages to DataFrame
            var dataFrame = await CreateDataFrameAsync(messageList, cancellationToken);
            
            // Write to Spark streaming sink (could be Kafka, file system, etc.)
            await WriteToStreamingSinkAsync(dataFrame, topic);

            _logger.LogDebug("Published {Count} messages to topic {Topic} via Spark", messageList.Count, topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch messages to topic {Topic}", topic);
            throw;
        }
    }

    public async Task SubscribeAsync(string topic, Func<T, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Subscribed to topic {Topic} with Spark streaming", topic);
            
            // In a real implementation, you'd set up a streaming query to read from the topic
            // For demonstration purposes, we'll just log the subscription
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to topic {Topic}", topic);
            throw;
        }
    }

    public async Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unsubscribed from topic {Topic}", topic);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from topic {Topic}", topic);
            throw;
        }
    }

    // ISparkMessageBroker interface methods
    public async Task<ISparkSession> GetSparkSessionAsync()
    {
        try
        {
            await Task.CompletedTask; // Make method truly async
            return new SparkSessionWrapper(_spark);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Spark session");
            throw;
        }
    }

    public async Task ExecuteSparkJobAsync(string jobName, Func<ISparkSession, Task> job, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSparkSessionAsync();
            await job(session);
            _logger.LogInformation("Executed Spark job: {JobName}", jobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Spark job: {JobName}", jobName);
            throw;
        }
    }

    public async Task<TResult> ExecuteSparkJobAsync<TResult>(string jobName, Func<ISparkSession, Task<TResult>> job, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await GetSparkSessionAsync();
            var result = await job(session);
            _logger.LogInformation("Executed Spark job: {JobName}", jobName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Spark job: {JobName}", jobName);
            throw;
        }
    }

    public async Task PublishToKafkaAsync(T message, string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = new[] { message };
            var dataFrame = await CreateDataFrameAsync(messages, cancellationToken);
            var kafkaUrl = $"kafka://localhost:9092/{topic}";
            
            if (dataFrame is SparkDataFrame<T> sparkDataFrame)
            {
                await WriteToKafkaAsync(sparkDataFrame.GetInternalDataFrame(), kafkaUrl);
            }
            
            _logger.LogDebug("Published message to Kafka topic {Topic}", topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing to Kafka topic {Topic}", topic);
            throw;
        }
    }

    public async Task<IEnumerable<T>> ReadFromKafkaAsync(string topic, CancellationToken cancellationToken = default)
    {
        try
        {
            var kafkaUrl = $"kafka://localhost:9092/{topic}";
            var dataFrame = await ReadFromSourceAsync(kafkaUrl, cancellationToken);
            var results = await dataFrame.CollectAsync();
            
            _logger.LogDebug("Read {Count} messages from Kafka topic {Topic}", results.Count(), topic);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from Kafka topic {Topic}", topic);
            throw;
        }
    }    public async Task<IDataFrame<T>> CreateDataFrameAsync(IEnumerable<T> data, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask; // Make async
            
            var dataList = data.ToList();
            if (!dataList.Any())
            {
                // Create empty dataframe with basic schema
                var emptySchema = new StructType(new[] { new StructField("empty", new StringType()) });
                var emptyData = new string[0];
                var emptyDf = _spark.CreateDataFrame(emptyData.Select(x => new GenericRow(new object[] { x })), emptySchema);
                return new SparkDataFrame<T>(emptyDf, _logger);
            }

            // Serialize data to JSON strings
            var jsonData = dataList.Select(item => JsonSerializer.Serialize(item, _jsonOptions)).ToArray();
            
            // Create DataFrame from JSON strings using simple approach
            var schema = new StructType(new[] { new StructField("json", new StringType()) });
            var rows = jsonData.Select(json => new GenericRow(new object[] { json }));
            var dataFrame = _spark.CreateDataFrame(rows, schema);

            _logger.LogDebug("Created Spark DataFrame with {Count} rows for type {Type}", dataList.Count, typeof(T).Name);
            
            return new SparkDataFrame<T>(dataFrame, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Spark DataFrame for type {Type}", typeof(T).Name);
            throw;
        }
    }

    public async Task<IDataFrame<T>> ReadFromSourceAsync(string source, CancellationToken cancellationToken = default)
    {
        try
        {
            DataFrame dataFrame;

            // Determine source type and read accordingly
            if (source.StartsWith("kafka://"))
            {
                dataFrame = await ReadFromKafkaSourceAsync(source);
            }
            else if (source.StartsWith("file://") || source.Contains("/") || source.Contains("\\"))
            {
                dataFrame = await ReadFromFileAsync(source);
            }
            else
            {
                throw new ArgumentException($"Unsupported source format: {source}");
            }

            _logger.LogDebug("Read DataFrame from source: {Source}", source);
            
            return new SparkDataFrame<T>(dataFrame, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from source {Source}", source);
            throw;
        }
    }

    public async Task WriteToSinkAsync(IDataFrame<T> dataFrame, string sink, CancellationToken cancellationToken = default)
    {
        try
        {
            if (dataFrame is not SparkDataFrame<T> sparkDataFrame)
            {
                throw new ArgumentException("DataFlow must be a SparkDataFrame instance");
            }

            var df = sparkDataFrame.GetInternalDataFrame();

            if (sink.StartsWith("kafka://"))
            {
                await WriteToKafkaAsync(df, sink);
            }
            else if (sink.StartsWith("file://") || sink.Contains("/") || sink.Contains("\\"))
            {
                await WriteToFileAsync(df, sink);
            }
            else
            {
                throw new ArgumentException($"Unsupported sink format: {sink}");
            }

            _logger.LogDebug("Wrote DataFrame to sink: {Sink}", sink);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing to sink {Sink}", sink);
            throw;
        }
    }

    // IDisposable interface
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _spark?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Spark session");
            }
            
            _disposed = true;
        }
    }

    // Private helper methods
    private async Task<DataFrame> ReadFromKafkaSourceAsync(string kafkaUrl)
    {
        await Task.CompletedTask; // Make async
        
        // Extract Kafka configuration from URL
        var uri = new Uri(kafkaUrl);
        var topic = uri.PathAndQuery.TrimStart('/');
        var bootstrapServers = $"{uri.Host}:{uri.Port}";

        return _spark
            .ReadStream()
            .Format("kafka")
            .Option("kafka.bootstrap.servers", bootstrapServers)
            .Option("subscribe", topic)
            .Load();
    }

    private async Task<DataFrame> ReadFromFileAsync(string filePath)
    {
        await Task.CompletedTask; // Make async
        
        var cleanPath = filePath.Replace("file://", "");
        
        if (Path.GetExtension(cleanPath).ToLowerInvariant() == ".json")
        {
            return _spark.Read().Json(cleanPath);
        }
        else if (Path.GetExtension(cleanPath).ToLowerInvariant() == ".csv")
        {
            return _spark.Read().Option("header", "true").Csv(cleanPath);
        }
        else
        {
            return _spark.Read().Text(cleanPath);
        }
    }

    private async Task WriteToStreamingSinkAsync(IDataFrame<T> dataFrame, string topic)
    {
        if (dataFrame is not SparkDataFrame<T> sparkDataFrame)
        {
            throw new ArgumentException("DataFlow must be a SparkDataFrame instance");
        }

        var df = sparkDataFrame.GetInternalDataFrame();
        
        // This is a simplified implementation
        // In production, you'd configure the appropriate streaming sink
        var query = df
            .WriteStream()
            .OutputMode("append")
            .Format("console") // Could be kafka, file, etc.
            .Start();

        // Let it run briefly for demonstration
        await Task.Delay(1000);
        query.Stop();
    }

    private async Task WriteToKafkaAsync(DataFrame dataFrame, string kafkaUrl)
    {
        await Task.CompletedTask; // Make async
        
        var uri = new Uri(kafkaUrl);
        var topic = uri.PathAndQuery.TrimStart('/');
        var bootstrapServers = $"{uri.Host}:{uri.Port}";

        dataFrame
            .Write()
            .Format("kafka")
            .Option("kafka.bootstrap.servers", bootstrapServers)
            .Option("topic", topic)
            .Save();
    }

    private async Task WriteToFileAsync(DataFrame dataFrame, string filePath)
    {
        await Task.CompletedTask; // Make async
        
        var cleanPath = filePath.Replace("file://", "");
        var extension = Path.GetExtension(cleanPath).ToLowerInvariant();

        switch (extension)
        {
            case ".json":
                dataFrame.Write().Mode("overwrite").Json(cleanPath);
                break;
            case ".csv":
                dataFrame.Write().Mode("overwrite").Option("header", "true").Csv(cleanPath);
                break;
            default:
                dataFrame.Write().Mode("overwrite").Text(cleanPath);
                break;
        }
    }
}

// Simple ISparkSession wrapper implementation
public class SparkSessionWrapper : ISparkSession
{
    private readonly SparkSession _session;    public string ApplicationId => "spark-app-" + Guid.NewGuid().ToString("N")[..8]; // Generate placeholder ID
    public string ApplicationName => "IoT Sensor Data Processor Spark Application";

    public SparkSessionWrapper(SparkSession session)
    {
        _session = session;
    }

    public async Task<IDataFrame<T>> CreateDataFrameAsync<T>(IEnumerable<T> data)
    {
        await Task.CompletedTask;
        
        // Simplified implementation - in practice you'd want better schema handling
        var jsonData = data.Select(item => JsonSerializer.Serialize(item)).ToArray();
        var schema = new StructType(new[] { new StructField("json", new StringType()) });
        var rows = jsonData.Select(json => new GenericRow(new object[] { json }));
        var df = _session.CreateDataFrame(rows, schema);
        
        return new SparkDataFrame<T>(df, null!); // Note: Logger is null here for simplicity
    }

    public async Task<IDataFrame<T>> ReadFromSourceAsync<T>(string source, IDictionary<string, string> options)
    {
        await Task.CompletedTask;
        
        var reader = _session.Read();
        
        if (options != null)
        {
            foreach (var option in options)
            {
                reader = reader.Option(option.Key, option.Value);
            }
        }
        
        var df = reader.Load(source);
        return new SparkDataFrame<T>(df, null!); // Note: Logger is null here for simplicity
    }

    public async Task WriteToSinkAsync<T>(IDataFrame<T> dataFrame, string sink, IDictionary<string, string> options)
    {
        await Task.CompletedTask;
        
        if (dataFrame is SparkDataFrame<T> sparkDf)
        {
            var writer = sparkDf.GetInternalDataFrame().Write();
            
            if (options != null)
            {
                foreach (var option in options)
                {
                    writer = writer.Option(option.Key, option.Value);
                }
            }
            
            writer.Save(sink);
        }
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await Task.CompletedTask;
        _session.Sql(sql);
    }

    public async Task<IDataFrame<T>> SqlAsync<T>(string sql)
    {
        await Task.CompletedTask;
        var df = _session.Sql(sql);
        return new SparkDataFrame<T>(df, null!); // Note: Logger is null here for simplicity
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
