namespace VibeCoding.Api.Domain.Enums;

public enum TelemetryEventType
{
    Generic = 0,
    ScenarioViewed = 1,
    ScenarioAttempted = 2,
    ScenarioCompleted = 3,
    ScenarioFeedbackSubmitted = 4,
    ImportStarted = 10,
    ImportCompleted = 11,
    ImportFailed = 12
}
