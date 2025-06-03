using IoTSensorDataProcessor.Core.Interfaces;
using IoTSensorDataProcessor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace IoTSensorDataProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorDataController : ControllerBase
{
    private readonly ISensorDataRepository _repository;
    private readonly ISensorDataProcessor _processor;
    private readonly IAnomalyDetectionService _anomalyService;
    private readonly ILogger<SensorDataController> _logger;

    public SensorDataController(
        ISensorDataRepository repository,
        ISensorDataProcessor processor,
        IAnomalyDetectionService anomalyService,
        ILogger<SensorDataController> logger)
    {
        _repository = repository;
        _processor = processor;
        _anomalyService = anomalyService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> PostSensorData([FromBody] SensorData sensorData)
    {
        try
        {
            await _processor.ProcessAsync(sensorData);
            return Ok(new { message = "Sensor data processed successfully", id = sensorData.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing sensor data");
            return StatusCode(500, new { message = "Error processing sensor data" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSensorData(string id)
    {
        try
        {
            var sensorData = await _repository.GetByIdAsync(id);
            if (sensorData == null)
                return NotFound();

            return Ok(sensorData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensor data");
            return StatusCode(500, new { message = "Error retrieving sensor data" });
        }
    }

    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetSensorDataByDevice(
        string deviceId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var sensorData = await _repository.GetByDeviceIdAsync(deviceId, from, to);
            return Ok(sensorData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensor data for device {DeviceId}", deviceId);
            return StatusCode(500, new { message = "Error retrieving sensor data" });
        }
    }

    [HttpGet("sensor-type/{sensorType}")]
    public async Task<IActionResult> GetSensorDataBySensorType(
        string sensorType,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var sensorData = await _repository.GetBySensorTypeAsync(sensorType, from, to);
            return Ok(sensorData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sensor data for sensor type {SensorType}", sensorType);
            return StatusCode(500, new { message = "Error retrieving sensor data" });
        }
    }

    [HttpGet("anomalies")]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] string? deviceId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var anomalies = await _anomalyService.GetAnomaliesAsync(deviceId, from, to);
            return Ok(anomalies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomalies");
            return StatusCode(500, new { message = "Error retrieving anomalies" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSensorData(string id)
    {
        try
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sensor data");
            return StatusCode(500, new { message = "Error deleting sensor data" });
        }
    }
}
