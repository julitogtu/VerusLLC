using Microsoft.AspNetCore.Mvc;

namespace VerusLLC.Api.Controllers;

[ApiController]
[Route("api/healthcheck")]
public class HealthCheckController : ControllerBase
{
    [HttpGet("/")]
    public IActionResult Get() => Ok();

    [HttpGet("/health")]
    public IActionResult Health() => Ok(new { status = "healthy" });
}
