namespace VibeCoding.Api.Contracts.Responses;

public class AuthResponse
{
    public required string AccessToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required string RefreshToken { get; init; }
}