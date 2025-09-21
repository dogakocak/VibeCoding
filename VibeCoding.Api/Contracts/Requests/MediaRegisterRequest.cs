namespace VibeCoding.Api.Contracts.Requests;

public class MediaRegisterRequest
{
    public string BlobName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
        = 0;
    public string Sha256Hash { get; set; } = string.Empty;
}