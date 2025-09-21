using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Domain.Entities;

public class TelemetryEvent
{
    public Guid Id { get; set; }
    public TelemetryEventType EventType { get; set; } = TelemetryEventType.Generic;
    public Guid? ScenarioId { get; set; }
        = null;
    public Guid? UserId { get; set; }
        = null;
    public string UserHash { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
        = null;
}
