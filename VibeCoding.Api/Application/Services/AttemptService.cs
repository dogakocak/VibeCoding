using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;
using VibeCoding.Api.Infrastructure.Data;

namespace VibeCoding.Api.Application.Services;

public class AttemptService : IAttemptService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<AttemptService> _logger;

    public AttemptService(ApplicationDbContext dbContext, ITelemetryService telemetryService, ILogger<AttemptService> logger)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public async Task<ScenarioAttempt> RecordAttemptAsync(AttemptCreateModel model, Guid userId, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.TrainingScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == model.ScenarioId, cancellationToken)
            ?? throw new KeyNotFoundException("Scenario not found");

        if (scenario.Status != ScenarioStatus.Published || scenario.IsArchived)
        {
            throw new InvalidOperationException("Scenario not available");
        }

        var score = scenario.CorrectOutcome == model.SelectedOutcome ? 1.0 : 0.0;
        var responseTime = model.ResponseTime <= TimeSpan.Zero
            ? TimeSpan.FromSeconds(1)
            : model.ResponseTime;

        var attempt = new ScenarioAttempt
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenario.Id,
            UserId = userId,
            SelectedOutcome = model.SelectedOutcome,
            Score = score,
            ConfidencePercent = Math.Clamp(model.ConfidencePercent, 0, 100),
            ResponseTime = responseTime,
            SessionId = string.IsNullOrWhiteSpace(model.SessionId) ? Guid.NewGuid().ToString("N") : model.SessionId.Trim(),
            Explanation = model.Explanation?.Trim(),
            CompletedAt = DateTimeOffset.UtcNow,
            IpHash = HashWithSalt(model.IpAddress, scenario.Id),
            UserAgentHash = HashWithSalt(model.UserAgent, scenario.Id)
        };

        await _dbContext.ScenarioAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            score,
            attempt.ConfidencePercent,
            attempt.ResponseTime,
            attempt.CompletedAt
        });

        await _telemetryService.RecordEventAsync(new TelemetryEventModel
        {
            EventType = TelemetryEventType.ScenarioAttempted,
            ScenarioId = scenario.Id,
            SessionId = attempt.SessionId,
            PayloadJson = payload
        }, userId, null, cancellationToken);

        _logger.LogInformation("User {UserId} attempted scenario {ScenarioId} with score {Score}", userId, scenario.Id, score);

        return attempt;
    }

    public async Task<IReadOnlyList<ScenarioAttempt>> GetAttemptsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ScenarioAttempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Include(a => a.Scenario!)
            .ThenInclude(s => s!.MediaAsset)
            .ToListAsync(cancellationToken);
    }

    private static string? HashWithSalt(string? value, Guid salt)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        using var sha = SHA256.Create();
        var input = Encoding.UTF8.GetBytes($"{value.Trim()}|{salt}");
        var hash = sha.ComputeHash(input);
        return Convert.ToHexString(hash);
    }
}

