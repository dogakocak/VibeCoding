namespace VibeCoding.Api.Application.Background;

public interface IBackgroundJobQueue
{
    ValueTask QueueAsync(BackgroundJobRequest request, CancellationToken cancellationToken = default);
    ValueTask<BackgroundJobRequest> DequeueAsync(CancellationToken cancellationToken);
}
