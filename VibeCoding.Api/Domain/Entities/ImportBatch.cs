using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Domain.Entities;

public class ImportBatch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceBlobName { get; set; } = string.Empty;
    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Draft;
    public Guid RequestedById { get; set; }
        = Guid.Empty;
    public ApplicationUser? RequestedBy { get; set; }
        = null;
    public int TotalRecords { get; set; }
        = 0;
    public int ProcessedRecords { get; set; }
        = 0;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessingStartedAt { get; set; }
        = null;
    public DateTimeOffset? CompletedAt { get; set; }
        = null;
    public string? FailureReason { get; set; }
        = null;

    public ICollection<ImportBatchLog> Logs { get; set; } = new List<ImportBatchLog>();
}
