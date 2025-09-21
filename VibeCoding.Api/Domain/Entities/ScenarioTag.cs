namespace VibeCoding.Api.Domain.Entities;

public class ScenarioTag
{
    public Guid ScenarioId { get; set; }
    public TrainingScenario? Scenario { get; set; }
        = null;
    public string Tag { get; set; } = string.Empty;
}
