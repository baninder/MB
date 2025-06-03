using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace IoTSensorDataProcessor.Infrastructure.Repositories;

public class CosmosDbSensorDataRepository : ISensorDataRepository
{
    private readonly Container _container;
    private const string DatabaseId = "IoTSensorData";
    private const string ContainerId = "SensorReadings";

    public CosmosDbSensorDataRepository(CosmosClient cosmosClient)
    {
        var database = cosmosClient.GetDatabase(DatabaseId);
        _container = database.GetContainer(ContainerId);
    }

    public async Task<string> SaveAsync(SensorData sensorData, CancellationToken cancellationToken = default)
    {
        var response = await _container.CreateItemAsync(
            sensorData,
            new PartitionKey(sensorData.DeviceId),
            cancellationToken: cancellationToken);

        return response.Resource.Id;
    }

    public async Task<SensorData?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<SensorData>(
                id,
                PartitionKey.None,
                cancellationToken: cancellationToken);

            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<SensorData>> GetByDeviceIdAsync(
        string deviceId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = _container.GetItemLinqQueryable<SensorData>()
            .Where(x => x.DeviceId == deviceId);

        if (from.HasValue)
            queryable = queryable.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            queryable = queryable.Where(x => x.Timestamp <= to.Value);

        using var feedIterator = queryable.ToFeedIterator();
        var results = new List<SensorData>();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IEnumerable<SensorData>> GetBySensorTypeAsync(
        string sensorType,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var queryable = _container.GetItemLinqQueryable<SensorData>()
            .Where(x => x.SensorType == sensorType);

        if (from.HasValue)
            queryable = queryable.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            queryable = queryable.Where(x => x.Timestamp <= to.Value);

        using var feedIterator = queryable.ToFeedIterator();
        var results = new List<SensorData>();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.DeleteItemAsync<SensorData>(
                id,
                PartitionKey.None,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
