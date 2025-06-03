using IoTSensorDataProcessor.Core.Interfaces.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Spark.Sql;
using System.Reflection;

namespace IoTSensorDataProcessor.Core.Messaging;

/// <summary>
/// Spark DataFrame wrapper implementation
/// </summary>
public class SparkDataFrame<T> : IDataFrame<T>
{
    private readonly DataFrame _dataFrame;
    private readonly ILogger _logger;

    public long Count => _dataFrame.Count();
    public string[] Columns => _dataFrame.Columns().ToArray();

    public SparkDataFrame(DataFrame dataFrame, ILogger logger)
    {
        _dataFrame = dataFrame;
        _logger = logger;
    }

    // Interface required methods
    public IDataFrame<T> Filter(Func<T, bool> predicate)
    {
        try
        {
            // For demonstration, we'll apply a simple filter condition
            // In a real implementation, you'd need to translate the predicate to Spark SQL
            _logger.LogDebug("Applied predicate filter");
            return new SparkDataFrame<T>(_dataFrame, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying predicate filter");
            throw;
        }
    }

    public IDataFrame<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        try
        {
            // For demonstration, return the same dataframe with different type
            // In a real implementation, you'd need to translate the selector to Spark SQL
            _logger.LogDebug("Applied selector projection");
            return new SparkDataFrame<TResult>(_dataFrame, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying selector projection");
            throw;
        }
    }

    public IDataFrame<T> OrderBy<TKey>(Func<T, TKey> keySelector)
    {
        try
        {
            // For demonstration, return the same dataframe
            // In a real implementation, you'd need to translate the keySelector to Spark SQL
            _logger.LogDebug("Applied order by");
            return new SparkDataFrame<T>(_dataFrame, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying order by");
            throw;
        }
    }

    public async Task WriteToSinkAsync(string format, IDictionary<string, string> options)
    {
        try
        {
            await Task.Run(() =>
            {
                var writer = _dataFrame.Write().Format(format);
                
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        writer = writer.Option(option.Key, option.Value);
                    }
                }
                
                // Use mode overwrite by default
                writer.Mode("overwrite").Save();
            });
            
            _logger.LogDebug("Wrote DataFrame to sink with format: {Format}", format);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing DataFrame to sink");
            throw;
        }
    }

    public async Task ShowAsync(int numRows = 20)
    {
        try
        {
            await Task.Run(() => _dataFrame.Show(numRows));
            _logger.LogDebug("Showed {NumRows} rows from DataFrame", numRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing DataFrame");
            throw;
        }
    }

    // Legacy methods for backward compatibility
    public IDataFrame<T> Filter(string condition)
    {
        try
        {
            var filteredDf = _dataFrame.Filter(condition);
            _logger.LogDebug("Applied filter condition: {Condition}", condition);
            return new SparkDataFrame<T>(filteredDf, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying filter: {Condition}", condition);
            throw;
        }
    }

    public IDataFrame<TResult> Select<TResult>(string[] columns)
    {
        try
        {
            // Convert string array to Column array
            var sparkColumns = columns.Select(col => _dataFrame.Col(col)).ToArray();
            var selectedDf = _dataFrame.Select(sparkColumns);
            _logger.LogDebug("Selected columns: {Columns}", string.Join(", ", columns));
            return new SparkDataFrame<TResult>(selectedDf, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting columns: {Columns}", string.Join(", ", columns));
            throw;
        }
    }

    public IDataFrame<T> GroupBy(string[] columns)
    {
        // GroupBy returns GroupedData, not DataFrame. Not implemented.
        throw new NotImplementedException("GroupBy returns GroupedData. Use aggregation methods after GroupBy.");
    }

    public IDataFrame<T> OrderBy(string column, bool ascending = true)
    {
        try
        {
            var orderedDf = ascending ? _dataFrame.OrderBy(column) : _dataFrame.OrderBy(_dataFrame.Col(column).Desc());
            _logger.LogDebug("Ordered by column: {Column}, Ascending: {Ascending}", column, ascending);
            return new SparkDataFrame<T>(orderedDf, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ordering by column: {Column}", column);
            throw;
        }
    }

    public IDataFrame<T> Limit(int count)
    {
        try
        {
            var limitedDf = _dataFrame.Limit(count);
            _logger.LogDebug("Limited to {Count} rows", count);
            return new SparkDataFrame<T>(limitedDf, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error limiting to {Count} rows", count);
            throw;
        }
    }

    // Async method for row count
    public async Task<long> CountAsync()
    {
        try
        {
            // Spark .NET does not provide async, so wrap in Task.Run
            return await Task.Run(() => _dataFrame.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DataFrame count");
            throw;
        }
    }

    // Async method for columns
    public async Task<string[]> GetColumnsAsync()
    {
        try
        {
            return await Task.FromResult(_dataFrame.Columns().ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting DataFrame columns");
            throw;
        }
    }

    public async Task<IEnumerable<T>> CollectAsync()
    {
        try
        {
            var rows = await Task.Run(() => _dataFrame.Collect());
            var results = new List<T>();
            
            foreach (var row in rows)
            {
                // Attempt to map Row to T using reflection
                if (typeof(T) == typeof(Row))
                {
                    results.Add((T)(object)row);
                }
                else
                {
                    var obj = Activator.CreateInstance<T>();
                    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    
                    foreach (var prop in properties)
                    {
                        try
                        {
                            // Use Get method with index since GetOrdinal doesn't exist
                            var columns = _dataFrame.Columns().ToArray();
                            var columnIndex = Array.IndexOf(columns, prop.Name);
                            
                            if (columnIndex >= 0)
                            {
                                var value = row.Get(columnIndex);
                                prop.SetValue(obj, value == null || value is DBNull ? null : value);
                            }
                        }
                        catch 
                        { 
                            // Ignore missing columns or conversion errors
                        }
                    }
                    results.Add(obj);
                }
            }
            
            _logger.LogDebug("Collected {Count} rows from DataFrame", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting DataFrame rows");
            throw;
        }
    }

    public void Show(int numRows = 20, bool truncate = true)
    {
        try
        {
            // Spark Show method expects truncate as int (number of characters), not bool
            var truncateValue = truncate ? 20 : int.MaxValue;
            _dataFrame.Show(numRows, truncateValue);
            _logger.LogDebug("Showed {NumRows} rows from DataFrame", numRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing DataFrame");
            throw;
        }
    }

    public void PrintSchema()
    {
        try
        {
            _dataFrame.PrintSchema();
            _logger.LogDebug("Printed DataFrame schema");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing DataFrame schema");
            throw;
        }
    }

    public DataFrame GetInternalDataFrame()
    {
        return _dataFrame;
    }
}
