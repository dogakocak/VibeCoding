using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Infrastructure.Options;

namespace VibeCoding.Api.Application.Services;

public class JwtTokenService : IJwtTokenService
{
    private const string RefreshTokenProvider = "VibeCoding";
    private const string RefreshTokenName = "RefreshToken";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _options;

    public JwtTokenService(UserManager<ApplicationUser> userManager, IOptions<JwtOptions> options)
    {
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken)> GenerateTokensAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: BuildSigningCredentials()
        );

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(jwt);
        var refreshToken = GenerateRefreshToken();
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, refreshToken);

        return (accessToken, expires, refreshToken);
    }

    public async Task<string?> RotateRefreshTokenAsync(ApplicationUser user, string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (storedToken is null)
        {
            return null;
        }

        if (!TryDecodeBase64(storedToken, out var storedBytes) || !TryDecodeBase64(refreshToken, out var providedBytes))
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes))
        {
            return null;
        }

        var newToken = GenerateRefreshToken();
        await _userManager.SetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName, newToken);
        return newToken;
    }

    private SigningCredentials BuildSigningCredentials()
        => new(BuildKey(), SecurityAlgorithms.HmacSha256);

    private SymmetricSecurityKey BuildKey()
        => new(Encoding.UTF8.GetBytes(_options.SecretKey));

    private static string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static bool TryDecodeBase64(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }
}
