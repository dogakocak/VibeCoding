namespace VibeCoding.Api.Contracts.Requests;

public class MediaUploadRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
}