using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Domain.Entities;

public class ScenarioAttempt
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
        = Guid.Empty;
    public TrainingScenario? Scenario { get; set; }
        = null;
    public Guid UserId { get; set; }
        = Guid.Empty;
    public ApplicationUser? User { get; set; }
        = null;
    public ScenarioOutcome SelectedOutcome { get; set; }
        = ScenarioOutcome.Real;
    public double Score { get; set; }
        = 0;
    public TimeSpan ResponseTime { get; set; }
        = TimeSpan.Zero;
    public int ConfidencePercent { get; set; }
        = 0;
    public string SessionId { get; set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Explanation { get; set; }
        = null;
    public string? IpHash { get; set; }
        = null;
    public string? UserAgentHash { get; set; }
        = null;
}
