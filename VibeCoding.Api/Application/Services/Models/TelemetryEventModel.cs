using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Application.Services.Models;

public class TelemetryEventModel
{
    public TelemetryEventType EventType { get; set; } = TelemetryEventType.Generic;
    public Guid? ScenarioId { get; set; }
        = null;
    public string SessionId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
