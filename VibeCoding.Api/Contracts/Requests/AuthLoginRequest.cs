namespace VibeCoding.Api.Contracts.Requests;

public class AuthLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}