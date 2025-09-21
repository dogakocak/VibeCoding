using Microsoft.AspNetCore.Mvc;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { Status = "Healthy", Timestamp = DateTimeOffset.UtcNow });
}