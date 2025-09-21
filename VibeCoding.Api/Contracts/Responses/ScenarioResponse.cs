using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Contracts.Responses;

public class ScenarioResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string Description { get; init; }
    public required ScenarioStatus Status { get; init; }
    public required ScenarioDifficulty Difficulty { get; init; }
    public required ScenarioOutcome CorrectOutcome { get; init; }
    public required Guid MediaAssetId { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
    public bool IsArchived { get; init; }
}