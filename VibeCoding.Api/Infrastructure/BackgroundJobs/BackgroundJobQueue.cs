using System.Threading.Channels;
using VibeCoding.Api.Application.Background;

namespace VibeCoding.Api.Infrastructure.BackgroundJobs;

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<BackgroundJobRequest> _queue;

    public BackgroundJobQueue(int capacity = 100)
    {
        _queue = Channel.CreateBounded<BackgroundJobRequest>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public ValueTask QueueAsync(BackgroundJobRequest request, CancellationToken cancellationToken = default)
        => _queue.Writer.WriteAsync(request, cancellationToken);

    public ValueTask<BackgroundJobRequest> DequeueAsync(CancellationToken cancellationToken)
        => _queue.Reader.ReadAsync(cancellationToken);
}
