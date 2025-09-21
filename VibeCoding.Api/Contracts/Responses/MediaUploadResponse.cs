namespace VibeCoding.Api.Contracts.Responses;

public class MediaUploadResponse
{
    public required string BlobName { get; init; }
    public required string UploadUrl { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}