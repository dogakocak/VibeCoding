namespace VibeCoding.Api.Domain.Entities;

public class MediaAsset
{
    public Guid Id { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string? ThumbnailBlobName { get; set; }
        = null;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
        = 0;
    public string Sha256Hash { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
        = Guid.Empty;
    public ApplicationUser? UploadedBy { get; set; }
        = null;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool RequiresThumbnail { get; set; } = false;

    public ICollection<TrainingScenario> Scenarios { get; set; } = new List<TrainingScenario>();
}
