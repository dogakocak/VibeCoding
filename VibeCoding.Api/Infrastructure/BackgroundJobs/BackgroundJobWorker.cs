using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Background;

namespace VibeCoding.Api.Infrastructure.BackgroundJobs;

public class BackgroundJobWorker : BackgroundService
{
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundJobWorker> _logger;

    public BackgroundJobWorker(IBackgroundJobQueue backgroundJobQueue, IServiceScopeFactory scopeFactory, ILogger<BackgroundJobWorker> logger)
    {
        _backgroundJobQueue = backgroundJobQueue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background job worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _backgroundJobQueue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job processing failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(BackgroundJobRequest job, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        _logger.LogInformation("Processing job {JobType} for {PrimaryId}", job.Type, job.PrimaryId);

        switch (job.Type)
        {
            case BackgroundJobType.ImportBatchProcess:
            {
                var service = scope.ServiceProvider.GetRequiredService<IImportService>();
                await service.ProcessImportAsync(job.PrimaryId, cancellationToken);
                break;
            }
            case BackgroundJobType.ThumbnailGeneration:
            {
                var mediaService = scope.ServiceProvider.GetRequiredService<IMediaService>();
                await mediaService.GenerateThumbnailAsync(job.PrimaryId, cancellationToken);
                break;
            }
            default:
                _logger.LogWarning("Unknown background job type {JobType}", job.Type);
                break;
        }
    }
}