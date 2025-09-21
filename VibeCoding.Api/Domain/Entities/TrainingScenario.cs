using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Domain.Entities;

public class TrainingScenario
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ScenarioDifficulty Difficulty { get; set; } = ScenarioDifficulty.Medium;
    public ScenarioStatus Status { get; set; } = ScenarioStatus.Draft;
    public ScenarioOutcome CorrectOutcome { get; set; } = ScenarioOutcome.Fake;
    public Guid MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }
        = null;
    public Guid CreatedById { get; set; }
        = Guid.Empty;
    public ApplicationUser? CreatedBy { get; set; }
        = null;
    public Guid? LastModifiedById { get; set; }
        = null;
    public ApplicationUser? LastModifiedBy { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAt { get; set; }
        = null;
    public bool IsArchived { get; set; } = false;
    public string? Notes { get; set; }
        = null;
    public string? ExternalReference { get; set; }
        = null;

    public ICollection<ScenarioTag> Tags { get; set; } = new List<ScenarioTag>();
    public ICollection<ScenarioAttempt> Attempts { get; set; } = new List<ScenarioAttempt>();

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
