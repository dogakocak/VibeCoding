using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Contracts.Requests;

public class AttemptCreateRequest
{
    public ScenarioOutcome SelectedOutcome { get; set; } = ScenarioOutcome.Real;
    public int ConfidencePercent { get; set; } = 0;
    public int ResponseTimeMilliseconds { get; set; } = 0;
    public string SessionId { get; set; } = string.Empty;
    public string? Explanation { get; set; }
        = null;
    public string? IpAddress { get; set; }
        = null;
    public string? UserAgent { get; set; }
        = null;
}