namespace VibeCoding.Api.Application.Services.Models;

public class MediaUploadToken
{
    public required string BlobName { get; init; }
    public required string UploadUrl { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
