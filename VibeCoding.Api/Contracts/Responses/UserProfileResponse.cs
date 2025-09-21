namespace VibeCoding.Api.Contracts.Responses;

public class UserProfileResponse
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}