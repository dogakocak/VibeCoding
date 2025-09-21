using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Application.Services.Models;

public class AttemptCreateModel
{
    public Guid ScenarioId { get; set; }
        = Guid.Empty;
    public ScenarioOutcome SelectedOutcome { get; set; }
        = ScenarioOutcome.Real;
    public int ConfidencePercent { get; set; }
        = 0;
    public TimeSpan ResponseTime { get; set; }
        = TimeSpan.Zero;
    public string SessionId { get; set; } = string.Empty;
    public string? Explanation { get; set; }
        = null;
    public string? IpAddress { get; set; }
        = null;
    public string? UserAgent { get; set; }
        = null;
}
