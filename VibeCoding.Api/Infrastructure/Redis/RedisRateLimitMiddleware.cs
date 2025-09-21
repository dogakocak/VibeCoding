using System.Security.Claims;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Infrastructure.Options;

namespace VibeCoding.Api.Infrastructure.Redis;

public class RedisRateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisRateLimitMiddleware> _logger;

    public RedisRateLimitMiddleware(RequestDelegate next, IConnectionMultiplexer connectionMultiplexer, IOptions<RedisOptions> options, ILogger<RedisRateLimitMiddleware> logger)
    {
        _next = next;
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.IsInRole(SystemRoles.Admin) ?? false)
        {
            await _next(context);
            return;
        }

        var key = BuildKey(context);
        var db = _connectionMultiplexer.GetDatabase();
        var windowSeconds = Math.Max(1, _options.DefaultSlidingWindowSeconds);
        var nowTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowStart = nowTicks - (windowSeconds * 1000);
        var redisKey = $"ratelimit:{key}";

        var batch = db.CreateBatch();
        var removeTask = batch.SortedSetRemoveRangeByScoreAsync(redisKey, double.NegativeInfinity, windowStart);
        var addTask = batch.SortedSetAddAsync(redisKey, Guid.NewGuid().ToString("N"), nowTicks);
        var expireTask = batch.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(windowSeconds));
        batch.Execute();
        await Task.WhenAll(removeTask, addTask, expireTask);

        var count = await db.SortedSetLengthAsync(redisKey);
        if (count > _options.DefaultPermitLimit)
        {
            _logger.LogWarning("Rate limit exceeded for {Key}", key);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = windowSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);
    }

    private static string BuildKey(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? context.User.Identity?.Name
                ?? "anonymous";
            return $"user:{subject}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ip}";
    }
}

