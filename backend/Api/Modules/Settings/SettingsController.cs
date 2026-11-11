using Cqrs.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Application.Modules.Settings;
using Api.Modules.Settings.Requests;
using Application.Modules.Settings.Queries;
using Application.Modules.Settings.Commands;

namespace Api.Modules.Settings;

[ApiController]
[Route("api/v1/settings")]
public sealed class SettingsController(IRequestDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppSettingsModel>> Get(CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new GetAppSettingsQuery(), cancellationToken));

    [HttpPut]
    public async Task<ActionResult<AppSettingsModel>> Update(
        UpdateAppSettingsRequest request,
        CancellationToken cancellationToken)
        => Ok(await dispatcher.Send(new UpdateAppSettingsCommand(
            request.ActiveProfileId,
            request.Culture,
            request.Theme,
            request.Palette,
            request.TimeZone,
            request.QueueCapacity,
            request.VisualizerSafeMargin), cancellationToken));
}
