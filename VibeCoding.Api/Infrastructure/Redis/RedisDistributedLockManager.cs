using StackExchange.Redis;
using VibeCoding.Api.Application.Abstractions;

namespace VibeCoding.Api.Infrastructure.Redis;

public class RedisDistributedLockManager : IDistributedLockManager
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisDistributedLockManager(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<IDisposable?> TryAcquireAsync(string resource, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var lockKey = $"locks:{resource}";
        var token = Guid.NewGuid().ToString("N");
        var acquired = await db.LockTakeAsync(lockKey, token, expiry);
        if (!acquired)
        {
            return null;
        }

        return new RedisLockHandle(db, lockKey, token);
    }

    private sealed class RedisLockHandle : IDisposable
    {
        private readonly IDatabase _db;
        private readonly string _key;
        private readonly string _token;
        private bool _disposed;

        public RedisLockHandle(IDatabase db, string key, string token)
        {
            _db = db;
            _key = key;
            _token = token;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _db.LockRelease(_key, _token);
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}