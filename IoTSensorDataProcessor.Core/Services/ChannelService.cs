using System.Threading.Channels;
using IoTSensorDataProcessor.Core.Interfaces;

namespace IoTSensorDataProcessor.Core.Services;

public class ChannelService : IChannelService
{
    private readonly Dictionary<string, object> _channels = new();
    private readonly object _lock = new();

    public Task<Channel<T>> GetChannelAsync<T>(string channelName)
    {
        lock (_lock)
        {
            if (_channels.TryGetValue(channelName, out var existingChannel))
            {
                return Task.FromResult((Channel<T>)existingChannel);
            }

            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            var channel = Channel.CreateBounded<T>(options);
            _channels[channelName] = channel;
            return Task.FromResult(channel);
        }
    }

    public async Task<ChannelWriter<T>> GetWriterAsync<T>(string channelName)
    {
        var channel = await GetChannelAsync<T>(channelName);
        return channel.Writer;
    }

    public async Task<ChannelReader<T>> GetReaderAsync<T>(string channelName)
    {
        var channel = await GetChannelAsync<T>(channelName);
        return channel.Reader;
    }
}
