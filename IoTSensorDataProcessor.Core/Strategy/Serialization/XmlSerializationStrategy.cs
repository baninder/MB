using IoTSensorDataProcessor.Core.Interfaces.Strategy;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace IoTSensorDataProcessor.Core.Strategy.Serialization;

/// <summary>
/// XML serialization strategy
/// </summary>
public class XmlSerializationStrategy<T> : ISerializationStrategy<T>
{
    private readonly ILogger<XmlSerializationStrategy<T>> _logger;
    private readonly XmlSerializer _serializer;
    private readonly XmlWriterSettings _writerSettings;
    private readonly XmlReaderSettings _readerSettings;

    public string ContentType => "application/xml";

    public XmlSerializationStrategy(ILogger<XmlSerializationStrategy<T>> logger)
    {
        _logger = logger;
        _serializer = new XmlSerializer(typeof(T));
        
        _writerSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = false,
            OmitXmlDeclaration = false
        };

        _readerSettings = new XmlReaderSettings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };
    }    public async Task<string> SerializeAsync(T data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null)
                return "<null />";

            // Use Task.Run for CPU-bound XML serialization work
            var xml = await Task.Run(() =>
            {
                using var stringWriter = new StringWriter();
                using var xmlWriter = XmlWriter.Create(stringWriter, _writerSettings);
                
                _serializer.Serialize(xmlWriter, data);
                return stringWriter.ToString();
            }, cancellationToken);
            
            _logger.LogDebug("Serialized {Type} to XML: {Length} characters", typeof(T).Name, xml.Length);
            
            return xml;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serializing {Type} to XML", typeof(T).Name);
            throw;
        }
    }    public async Task<T?> DeserializeAsync(string data, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(data) || data.Trim() == "<null />")
                return default;

            // Use Task.Run for CPU-bound XML deserialization work
            var result = await Task.Run(() =>
            {
                using var stringReader = new StringReader(data);
                using var xmlReader = XmlReader.Create(stringReader, _readerSettings);
                
                return (T?)_serializer.Deserialize(xmlReader);
            }, cancellationToken);
            
            _logger.LogDebug("Deserialized XML to {Type}", typeof(T).Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing XML to {Type}: {Data}", typeof(T).Name, data);
            throw;
        }
    }

    public bool SupportsType(Type type)
    {
        // XML serialization requires public parameterless constructor and serializable properties
        try
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            return constructor != null && constructor.IsPublic;
        }
        catch
        {
            return false;
        }
    }
}
