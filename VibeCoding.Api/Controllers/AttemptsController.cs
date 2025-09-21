using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Contracts.Requests;
using VibeCoding.Api.Contracts.Responses;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class AttemptsController : ControllerBase
{
    private readonly IAttemptService _attemptService;
    private readonly ICurrentUserContext _currentUserContext;

    public AttemptsController(IAttemptService attemptService, ICurrentUserContext currentUserContext)
    {
        _attemptService = attemptService;
        _currentUserContext = currentUserContext;
    }

    [HttpPost("scenarios/{scenarioId:guid}/attempts")]
    public async Task<ActionResult<AttemptResponse>> RecordAttempt(Guid scenarioId, [FromBody] AttemptCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var model = new AttemptCreateModel
        {
            ScenarioId = scenarioId,
            SelectedOutcome = request.SelectedOutcome,
            ConfidencePercent = request.ConfidencePercent,
            ResponseTime = TimeSpan.FromMilliseconds(request.ResponseTimeMilliseconds),
            SessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString("N") : request.SessionId,
            Explanation = request.Explanation,
            IpAddress = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.UserAgent ?? Request.Headers["User-Agent"].ToString()
        };

        var attempt = await _attemptService.RecordAttemptAsync(model, _currentUserContext.UserId.Value, cancellationToken);
        return Ok(ToResponse(attempt));
    }

    [HttpGet("attempts/me")]
    public async Task<ActionResult<IEnumerable<AttemptResponse>>> GetMyAttempts(CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var attempts = await _attemptService.GetAttemptsForUserAsync(_currentUserContext.UserId.Value, cancellationToken);
        return Ok(attempts.Select(ToResponse));
    }

    private static AttemptResponse ToResponse(VibeCoding.Api.Domain.Entities.ScenarioAttempt attempt)
        => new()
        {
            Id = attempt.Id,
            ScenarioId = attempt.ScenarioId,
            SelectedOutcome = attempt.SelectedOutcome,
            Score = attempt.Score,
            ConfidencePercent = attempt.ConfidencePercent,
            ResponseTimeMilliseconds = (int)attempt.ResponseTime.TotalMilliseconds,
            CompletedAt = attempt.CompletedAt,
            Explanation = attempt.Explanation
        };
}
