using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Background;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Infrastructure.Data;
using VibeCoding.Api.Infrastructure.Options;

namespace VibeCoding.Api.Application.Services;

public class MediaService : IMediaService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BlobContainerClient _containerClient;
    private readonly AzureBlobOptions _options;
    private readonly IBackgroundJobQueue _backgroundJobQueue;
    private readonly ILogger<MediaService> _logger;

    public MediaService(BlobServiceClient blobServiceClient, ApplicationDbContext dbContext, IOptions<AzureBlobOptions> options, IBackgroundJobQueue backgroundJobQueue, ILogger<MediaService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _backgroundJobQueue = backgroundJobQueue;
        _logger = logger;
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task<MediaUploadToken> CreateUploadTokenAsync(string fileName, string contentType, Guid uploadedBy, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var sanitizedName = SanitizeFileName(fileName);
        var blobName = $"uploads/{DateTime.UtcNow:yyyy/MM/dd}/{uploadedBy}/{Guid.NewGuid():N}-{sanitizedName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!blobClient.CanGenerateSasUri)
        {
            throw new InvalidOperationException("Blob client cannot generate SAS URI; ensure credential contains account key");
        }

        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.AssetWriteExpiryMinutes);
        var builder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = expiresOn,
            ContentType = contentType
        };
        builder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var sasUri = blobClient.GenerateSasUri(builder);

        return new MediaUploadToken
        {
            BlobName = blobName,
            UploadUrl = sasUri.ToString(),
            ExpiresAt = expiresOn
        };
    }

    public async Task<MediaAsset> RegisterAssetAsync(string blobName, string fileName, string contentType, long fileSizeBytes, string sha256Hash, Guid uploadedBy, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException("Blob not found", blobName);
        }

        BlobProperties properties;
        try
        {
            properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to fetch blob properties for {BlobName}", blobName);
            throw;
        }

        if (properties.ContentLength != fileSizeBytes)
        {
            throw new InvalidOperationException("Uploaded blob size mismatch");
        }

        var now = DateTimeOffset.UtcNow;
        var asset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            BlobName = blobName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? properties.ContentType ?? "application/octet-stream" : contentType,
            FileSizeBytes = fileSizeBytes,
            Sha256Hash = sha256Hash,
            UploadedById = uploadedBy,
            CreatedAt = now,
            RequiresThumbnail = ShouldGenerateThumbnail(contentType)
        };

        await _dbContext.MediaAssets.AddAsync(asset, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (asset.RequiresThumbnail)
        {
            await QueueThumbnailGenerationAsync(asset.Id, cancellationToken);
        }

        return asset;
    }

    public async Task<(string Url, DateTimeOffset ExpiresAt)> GetReadSasUrlAsync(MediaAsset asset, CancellationToken cancellationToken = default)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = _containerClient.GetBlobClient(asset.ThumbnailBlobName ?? asset.BlobName);
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.AssetReadExpiryMinutes);

        if (!blobClient.CanGenerateSasUri)
        {
            var uri = blobClient.Uri;
            if (!string.IsNullOrWhiteSpace(_options.PublicCdnHost))
            {
                var target = new Uri(_options.PublicCdnHost, UriKind.Absolute);
                var builder = new UriBuilder(uri)
                {
                    Host = target.Host,
                    Scheme = target.Scheme,
                    Port = target.IsDefaultPort ? -1 : target.Port
                };
                uri = builder.Uri;
            }
            return (uri.ToString(), expiresOn);
        }

        var builderSas = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = expiresOn
        };
        builderSas.SetPermissions(BlobSasPermissions.Read);

        return (blobClient.GenerateSasUri(builderSas).ToString(), expiresOn);
    }

    public Task QueueThumbnailGenerationAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        return _backgroundJobQueue.QueueAsync(new BackgroundJobRequest(BackgroundJobType.ThumbnailGeneration, mediaAssetId), cancellationToken).AsTask();
    }

    public async Task GenerateThumbnailAsync(Guid mediaAssetId, CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == mediaAssetId, cancellationToken);
        if (asset is null)
        {
            _logger.LogWarning("Media asset {AssetId} not found for thumbnail", mediaAssetId);
            return;
        }

        if (!asset.RequiresThumbnail)
        {
            return;
        }

        var blobClient = _containerClient.GetBlobClient(asset.BlobName);
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("Source blob missing for asset {AssetId}", mediaAssetId);
            return;
        }

        try
        {
            await using var sourceStream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            using var image = await Image.LoadAsync(sourceStream, cancellationToken);
            const int targetSize = 256;
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(targetSize, targetSize)
            }));

            var thumbName = $"thumbnails/{asset.Id:N}.png";
            var thumbClient = _containerClient.GetBlobClient(thumbName);
            await using var output = new MemoryStream();
            await image.SaveAsync(output, new PngEncoder(), cancellationToken);
            output.Position = 0;
            await thumbClient.UploadAsync(output, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/png" }
            }, cancellationToken);

            asset.ThumbnailBlobName = thumbName;
            asset.RequiresThumbnail = false;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Generated thumbnail for asset {AssetId}", mediaAssetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for asset {AssetId}", mediaAssetId);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(fileName.Where(ch => !invalid.Contains(ch)).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
    }

    private static bool ShouldGenerateThumbnail(string contentType)
        => contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}


