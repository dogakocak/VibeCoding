using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Contracts.Requests;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;
    private readonly ICurrentUserContext _currentUserContext;

    public TelemetryController(ITelemetryService telemetryService, ICurrentUserContext currentUserContext)
    {
        _telemetryService = telemetryService;
        _currentUserContext = currentUserContext;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Record([FromBody] TelemetryEventRequest request, CancellationToken cancellationToken)
    {
        var model = new TelemetryEventModel
        {
            EventType = request.EventType,
            ScenarioId = request.ScenarioId,
            SessionId = request.SessionId,
            PayloadJson = request.PayloadJson
        };

        await _telemetryService.RecordEventAsync(model, _currentUserContext.UserId, _currentUserContext.Email, cancellationToken);
        return Accepted();
    }
}