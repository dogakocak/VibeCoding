using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Application.Abstractions;

public interface IMediaService
{
    Task<MediaUploadToken> CreateUploadTokenAsync(string fileName, string contentType, Guid uploadedBy, CancellationToken cancellationToken = default);
    Task<MediaAsset> RegisterAssetAsync(string blobName, string fileName, string contentType, long fileSizeBytes, string sha256Hash, Guid uploadedBy, CancellationToken cancellationToken = default);
    Task<(string Url, DateTimeOffset ExpiresAt)> GetReadSasUrlAsync(MediaAsset asset, CancellationToken cancellationToken = default);
    Task QueueThumbnailGenerationAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
    Task GenerateThumbnailAsync(Guid mediaAssetId, CancellationToken cancellationToken = default);
}