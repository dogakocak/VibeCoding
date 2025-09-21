using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Contracts.Requests;
using VibeCoding.Api.Contracts.Responses;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenProvider = "VibeCoding";
    private const string RefreshTokenName = "RefreshToken";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] AuthRegisterRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return Conflict("Email already registered");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        await _userManager.AddToRoleAsync(user, SystemRoles.Learner);
        var (accessToken, expiresAt, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user, cancellationToken);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] AuthLoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized();
        }

        var (accessToken, expiresAt, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user, cancellationToken);
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var storedToken = await _userManager.GetAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
        if (storedToken is null || !TokensMatch(storedToken, request.RefreshToken))
        {
            return Unauthorized();
        }

        var (accessToken, expiresAt, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user, cancellationToken);
        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt,
            RefreshToken = refreshToken
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Roles = roles.ToArray()
        });
    }

    private static bool TokensMatch(string stored, string provided)
    {
        try
        {
            var storedBytes = Convert.FromBase64String(stored);
            var providedBytes = Convert.FromBase64String(provided);
            return storedBytes.Length == providedBytes.Length && CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
