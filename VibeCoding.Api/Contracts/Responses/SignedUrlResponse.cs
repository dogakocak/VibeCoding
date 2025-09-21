namespace VibeCoding.Api.Contracts.Responses;

public class SignedUrlResponse
{
    public required string Url { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}