using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Contracts.Responses;

public class ImportBatchResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required ImportBatchStatus Status { get; init; }
    public required int TotalRecords { get; init; }
    public required int ProcessedRecords { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ProcessingStartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? FailureReason { get; init; }
    public required IReadOnlyCollection<ImportLogResponse> Logs { get; init; }
}