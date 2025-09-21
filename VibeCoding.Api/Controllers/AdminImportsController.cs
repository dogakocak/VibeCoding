using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Contracts.Requests;
using VibeCoding.Api.Contracts.Responses;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api/admin/imports")]
[Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
public class AdminImportsController : ControllerBase
{
    private readonly IImportService _importService;
    private readonly ICurrentUserContext _currentUserContext;

    public AdminImportsController(IImportService importService, ICurrentUserContext currentUserContext)
    {
        _importService = importService;
        _currentUserContext = currentUserContext;
    }

    [HttpPost]
    public async Task<ActionResult<ImportBatchResponse>> CreateImport([FromBody] ImportCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUserContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var model = new ImportRequestModel
        {
            Name = request.Name,
            SourceBlobName = request.SourceBlobName ?? string.Empty,
            Scenarios = request.Scenarios.Select(s => new ImportScenarioDefinition
            {
                Title = s.Title,
                Description = s.Description,
                Difficulty = s.Difficulty,
                CorrectOutcome = s.CorrectOutcome,
                MediaBlobName = s.MediaBlobName,
                Tags = s.Tags,
                ExternalReference = s.ExternalReference
            }).ToList()
        };

        var batch = await _importService.CreateImportAsync(model, _currentUserContext.UserId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetImports), new { }, ToResponse(batch));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ImportBatchResponse>>> GetImports(CancellationToken cancellationToken)
    {
        var imports = await _importService.GetImportsAsync(cancellationToken);
        return Ok(imports.Select(ToResponse));
    }

    [HttpPost("{id:guid}/queue")]
    public async Task<IActionResult> Queue(Guid id, CancellationToken cancellationToken)
    {
        await _importService.QueueImportProcessingAsync(id, cancellationToken);
        return Accepted();
    }

    private static ImportBatchResponse ToResponse(ImportBatch batch)
        => new()
        {
            Id = batch.Id,
            Name = batch.Name,
            Status = batch.Status,
            TotalRecords = batch.TotalRecords,
            ProcessedRecords = batch.ProcessedRecords,
            CreatedAt = batch.CreatedAt,
            ProcessingStartedAt = batch.ProcessingStartedAt,
            CompletedAt = batch.CompletedAt,
            FailureReason = batch.FailureReason,
            Logs = batch.Logs
                .OrderBy(l => l.LoggedAt)
                .Select(l => new ImportLogResponse
                {
                    LoggedAt = l.LoggedAt,
                    Level = l.Level,
                    Message = l.Message
                })
                .ToList()
        };
}