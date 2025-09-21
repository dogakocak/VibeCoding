using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Contracts.Responses;

public class AttemptResponse
{
    public required Guid Id { get; init; }
    public required Guid ScenarioId { get; init; }
    public required ScenarioOutcome SelectedOutcome { get; init; }
    public required double Score { get; init; }
    public required int ConfidencePercent { get; init; }
    public required int ResponseTimeMilliseconds { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public string? Explanation { get; init; }
}