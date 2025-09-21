namespace VibeCoding.Api.Domain.Entities;

public class ImportBatchLog
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
        = Guid.Empty;
    public ImportBatch? ImportBatch { get; set; }
        = null;
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = string.Empty;
}
