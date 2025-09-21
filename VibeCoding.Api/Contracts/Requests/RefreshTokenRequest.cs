namespace VibeCoding.Api.Contracts.Requests;

public class RefreshTokenRequest
{
    public string Email { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}