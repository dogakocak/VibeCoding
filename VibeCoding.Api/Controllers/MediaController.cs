using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Contracts.Requests;
using VibeCoding.Api.Contracts.Responses;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Infrastructure.Data;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ApplicationDbContext _dbContext;

    public MediaController(IMediaService mediaService, ICurrentUserContext currentUserContext, ApplicationDbContext dbContext)
    {
        _mediaService = mediaService;
        _currentUserContext = currentUserContext;
        _dbContext = dbContext;
    }

    [HttpPost("upload-url")]
    [Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
    public async Task<ActionResult<MediaUploadResponse>> CreateUploadUrl([FromBody] MediaUploadRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var token = await _mediaService.CreateUploadTokenAsync(request.FileName, request.ContentType, _currentUserContext.UserId.Value, cancellationToken);
        return Ok(new MediaUploadResponse
        {
            BlobName = token.BlobName,
            UploadUrl = token.UploadUrl,
            ExpiresAt = token.ExpiresAt
        });
    }

    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
    public async Task<ActionResult<MediaAssetResponse>> Register([FromBody] MediaRegisterRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var asset = await _mediaService.RegisterAssetAsync(request.BlobName, request.FileName, request.ContentType, request.FileSizeBytes, request.Sha256Hash, _currentUserContext.UserId.Value, cancellationToken);
        return Ok(new MediaAssetResponse
        {
            Id = asset.Id,
            BlobName = asset.BlobName,
            ThumbnailBlobName = asset.ThumbnailBlobName,
            ContentType = asset.ContentType,
            FileSizeBytes = asset.FileSizeBytes
        });
    }

    [HttpGet("{id:guid}/signed-url")]
    [Authorize]
    public async Task<ActionResult<SignedUrlResponse>> GetSignedUrl(Guid id, CancellationToken cancellationToken)
    {
        var asset = await _dbContext.MediaAssets.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var (url, expiresAt) = await _mediaService.GetReadSasUrlAsync(asset, cancellationToken);
        return Ok(new SignedUrlResponse
        {
            Url = url,
            ExpiresAt = expiresAt
        });
    }
}