namespace VibeCoding.Api.Contracts.Responses;

public class MediaAssetResponse
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public string? ThumbnailBlobName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSizeBytes { get; init; }
}