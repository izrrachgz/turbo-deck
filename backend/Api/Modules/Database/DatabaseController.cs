using Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Database;

[ApiController]
[Route("system/database")]
public sealed class DatabaseController : ControllerBase
{
    private DatabaseAdministrationService Databases => HttpContext.RequestServices.GetRequiredService<DatabaseAdministrationService>();

    [HttpGet]
    [ProducesResponseType<DatabaseSettingsResponse>(StatusCodes.Status200OK)]
    public ActionResult<DatabaseSettingsResponse> Get() => Ok(Databases.GetSettings());

    [HttpPost("test")]
    [ProducesResponseType<DatabaseConnectionTestResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseConnectionTestResponse>> Test(
        DatabaseConnectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await Databases.TestAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid database configuration", Detail = exception.Message, Status = 400 });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Database is not configured", Detail = exception.Message, Status = 400 });
        }
    }

    [HttpPost("switch")]
    [ProducesResponseType<DatabaseSwitchResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseSwitchResponse>> Switch(
        DatabaseConnectionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await Databases.SwitchAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid database configuration", Detail = exception.Message, Status = 400 });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Database is not configured", Detail = exception.Message, Status = 400 });
        }
        catch (DatabaseSwitchException exception)
        {
            return Conflict(new ProblemDetails { Title = "Database switch failed", Detail = exception.Message, Status = 409 });
        }
    }
}
