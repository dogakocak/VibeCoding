using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Infrastructure.Data;
using VibeCoding.Api.Infrastructure.Options;

namespace VibeCoding.Api.Application.Services;

public class TelemetryService : ITelemetryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TelemetryOptions _options;
    private readonly ILogger<TelemetryService> _logger;

    public TelemetryService(ApplicationDbContext dbContext, IOptions<TelemetryOptions> options, ILogger<TelemetryService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RecordEventAsync(TelemetryEventModel model, Guid? userId, string? userEmail, CancellationToken cancellationToken = default)
    {
        var telemetryEvent = new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            EventType = model.EventType,
            ScenarioId = model.ScenarioId,
            UserId = userId,
            UserHash = BuildUserHash(userId, userEmail, model.SessionId),
            SessionId = model.SessionId,
            Payload = model.PayloadJson,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        await _dbContext.TelemetryEvents.AddAsync(telemetryEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Telemetry event {EventType} recorded", model.EventType);
    }

    private string BuildUserHash(Guid? userId, string? email, string sessionId)
    {
        var basis = userId?.ToString() ?? email ?? sessionId;
        var salted = string.Concat(basis, "|", _options.UserHashSalt);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(hash);
    }
}