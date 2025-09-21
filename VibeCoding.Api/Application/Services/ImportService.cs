using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Background;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;
using VibeCoding.Api.Infrastructure.Data;
using VibeCoding.Api.Infrastructure.Options;

namespace VibeCoding.Api.Application.Services;

public class ImportService : IImportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IScenarioService _scenarioService;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly IDistributedLockManager _lockManager;
    private readonly ILogger<ImportService> _logger;
    private readonly AzureBlobOptions _blobOptions;
    private readonly BlobServiceClient _blobServiceClient;

    public ImportService(
        ApplicationDbContext dbContext,
        IScenarioService scenarioService,
        IBackgroundJobQueue backgroundJobQueue,
        IDistributedLockManager lockManager,
        IOptions<AzureBlobOptions> blobOptions,
        BlobServiceClient blobServiceClient,
        ILogger<ImportService> logger)
    {
        _dbContext = dbContext;
        _scenarioService = scenarioService;
        _backgroundJobQueue = backgroundJobQueue;
        _lockManager = lockManager;
        _logger = logger;
        _blobOptions = blobOptions.Value;
        _blobServiceClient = blobServiceClient;
    }

    public async Task<ImportBatch> CreateImportAsync(ImportRequestModel request, Guid requestedById, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceBlobName) && !request.Scenarios.Any())
        {
            throw new ArgumentException("Either SourceBlobName or inline scenarios must be provided", nameof(request));
        }

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            SourceBlobName = request.SourceBlobName,
            Status = ImportBatchStatus.Draft,
            RequestedById = requestedById,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _dbContext.ImportBatches.AddAsync(batch, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await AddLogAsync(batch, "Draft import created", cancellationToken);

        if (request.Scenarios.Any())
        {
            var manifestBlob = await PersistInlineManifestAsync(batch, request.Scenarios, cancellationToken);
            batch.SourceBlobName = manifestBlob;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AddLogAsync(batch, $"Embedded manifest persisted to {manifestBlob}", cancellationToken);
        }

        return batch;
    }

    public async Task<IReadOnlyList<ImportBatch>> GetImportsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ImportBatches
            .Include(x => x.Logs)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task QueueImportProcessingAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var batch = await _dbContext.ImportBatches.FirstOrDefaultAsync(x => x.Id == importBatchId, cancellationToken)
            ?? throw new KeyNotFoundException("Import batch not found");

        if (batch.Status is ImportBatchStatus.Processing or ImportBatchStatus.Completed)
        {
            return;
        }

        batch.Status = ImportBatchStatus.Queued;
        batch.ProcessingStartedAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AddLogAsync(batch, "Import queued for processing", cancellationToken);

        await _backgroundJobQueue.QueueAsync(new BackgroundJobRequest(BackgroundJobType.ImportBatchProcess, importBatchId), cancellationToken);
    }

    public async Task ProcessImportAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var lease = await _lockManager.TryAcquireAsync($"import:{importBatchId}", TimeSpan.FromMinutes(5), cancellationToken);
        if (lease is null)
        {
            _logger.LogWarning("Could not acquire lock for import {ImportId}", importBatchId);
            return;
        }

        using (lease)
        {
            var batch = await _dbContext.ImportBatches.Include(x => x.Logs)
                .FirstOrDefaultAsync(x => x.Id == importBatchId, cancellationToken)
                ?? throw new KeyNotFoundException("Import batch not found");

            if (batch.Status == ImportBatchStatus.Completed)
            {
                return;
            }

            batch.Status = ImportBatchStatus.Processing;
            batch.ProcessingStartedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AddLogAsync(batch, "Import processing started", cancellationToken);

            try
            {
                var definitions = await LoadDefinitionsAsync(batch.SourceBlobName, cancellationToken);
                batch.TotalRecords = definitions.Count;
                var success = 0;

                foreach (var definition in definitions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var media = await _dbContext.MediaAssets.FirstOrDefaultAsync(m => m.BlobName == definition.MediaBlobName, cancellationToken);
                        if (media is null)
                        {
                            await AddLogAsync(batch, $"Missing media asset for blob {definition.MediaBlobName}", cancellationToken, "Warning");
                            continue;
                        }

                        var model = new ScenarioUpsertModel
                        {
                            Title = definition.Title,
                            Description = definition.Description,
                            Difficulty = definition.Difficulty,
                            CorrectOutcome = definition.CorrectOutcome,
                            MediaAssetId = media.Id,
                            Tags = definition.Tags,
                            ExternalReference = definition.ExternalReference
                        };

                        await _scenarioService.CreateAsync(model, batch.RequestedById, cancellationToken);
                        success++;
                        batch.ProcessedRecords = success;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to import scenario {Title}", definition.Title);
                        await AddLogAsync(batch, $"Failed to import {definition.Title}: {ex.Message}", cancellationToken, "Error");
                    }
                }

                batch.Status = ImportBatchStatus.Completed;
                batch.CompletedAt = DateTimeOffset.UtcNow;
                await AddLogAsync(batch, $"Import completed with {batch.ProcessedRecords}/{batch.TotalRecords} scenarios", cancellationToken, "Info");
            }
            catch (Exception ex)
            {
                batch.Status = ImportBatchStatus.Failed;
                batch.CompletedAt = DateTimeOffset.UtcNow;
                batch.FailureReason = ex.Message;
                await AddLogAsync(batch, $"Import failed: {ex.Message}", cancellationToken, "Error");
                _logger.LogError(ex, "Import {ImportId} failed", importBatchId);
            }
            finally
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task<string> PersistInlineManifestAsync(ImportBatch batch, IEnumerable<ImportScenarioDefinition> scenarios, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_blobOptions.ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobName = $"manifests/{batch.Id:N}.json";
        var blob = container.GetBlobClient(blobName);

        var payload = JsonSerializer.Serialize(scenarios, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: cancellationToken);
        return blobName;
    }

    private async Task<List<ImportScenarioDefinition>> LoadDefinitionsAsync(string blobName, CancellationToken cancellationToken)
    {
        var container = _blobServiceClient.GetBlobContainerClient(_blobOptions.ContainerName);
        var blob = container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException("Manifest blob not found", blobName);
        }

        await using var stream = await blob.OpenReadAsync(cancellationToken: cancellationToken);
        return await JsonSerializer.DeserializeAsync<List<ImportScenarioDefinition>>(stream, cancellationToken: cancellationToken)
            ?? new List<ImportScenarioDefinition>();
    }

    private async Task AddLogAsync(ImportBatch batch, string message, CancellationToken cancellationToken, string level = "Info")
    {
        batch.Logs.Add(new ImportBatchLog
        {
            Id = Guid.NewGuid(),
            ImportBatchId = batch.Id,
            Level = level,
            Message = message,
            LoggedAt = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

