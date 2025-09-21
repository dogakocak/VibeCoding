using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Application.Abstractions;

public interface IJwtTokenService
{
    Task<(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken)> GenerateTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    Task<string?> RotateRefreshTokenAsync(ApplicationUser user, string refreshToken, CancellationToken cancellationToken = default);
}
