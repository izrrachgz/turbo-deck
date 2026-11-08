using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Health;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("live")]
    public ActionResult<object> Live() => Ok(new
    {
        status = "live",
        version = typeof(HealthController).Assembly.GetName().Version?.ToString()
    });

    [HttpGet("ready")]
    public ActionResult<object> Ready() => Ok(new
    {
        status = "ready",
        version = typeof(HealthController).Assembly.GetName().Version?.ToString()
    });
}
