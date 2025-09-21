namespace VibeCoding.Api.Application.Abstractions;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsInRole(string role);
}
