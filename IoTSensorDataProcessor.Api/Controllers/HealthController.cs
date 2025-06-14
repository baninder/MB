using Microsoft.AspNetCore.Mvc;

namespace IoTSensorDataProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            service = "IoT Sensor Data Processor"
        });
    }
}
