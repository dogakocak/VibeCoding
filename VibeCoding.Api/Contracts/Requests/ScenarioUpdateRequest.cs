using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Contracts.Requests;

public class ScenarioUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ScenarioDifficulty Difficulty { get; set; } = ScenarioDifficulty.Medium;
    public ScenarioOutcome CorrectOutcome { get; set; } = ScenarioOutcome.Fake;
    public Guid MediaAssetId { get; set; }
        = Guid.Empty;
    public IList<string> Tags { get; set; } = new List<string>();
    public string? Notes { get; set; }
        = null;
    public string? ExternalReference { get; set; }
        = null;
}