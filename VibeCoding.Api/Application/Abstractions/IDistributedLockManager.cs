namespace VibeCoding.Api.Application.Abstractions;

public interface IDistributedLockManager
{
    Task<IDisposable?> TryAcquireAsync(string resource, TimeSpan expiry, CancellationToken cancellationToken = default);
}
