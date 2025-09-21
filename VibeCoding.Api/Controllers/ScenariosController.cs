using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Contracts.Requests;
using VibeCoding.Api.Contracts.Responses;
using VibeCoding.Api.Domain.Constants;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly IScenarioService _scenarioService;
    private readonly IMediaService _mediaService;
    private readonly ICurrentUserContext _currentUser;

    public ScenariosController(IScenarioService scenarioService, IMediaService mediaService, ICurrentUserContext currentUser)
    {
        _scenarioService = scenarioService;
        _mediaService = mediaService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ScenarioResponse>>> GetScenarios([FromQuery] ScenarioQueryRequest request, CancellationToken cancellationToken)
    {
        var query = new ScenarioQueryOptions
        {
            Status = request.Status,
            Difficulty = request.Difficulty,
            Search = request.Search,
            IncludeArchived = request.IncludeArchived && User.IsInRole(SystemRoles.Admin),
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _scenarioService.GetPagedAsync(query, cancellationToken);
        var response = new PagedResponse<ScenarioResponse>
        {
            Items = result.Items.Select(ToResponse).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
        return Ok(response);
    }

    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ScenarioResponse>>> GetFeatured([FromQuery] int count = 4, CancellationToken cancellationToken = default)
    {
        var scenarios = await _scenarioService.GetFeaturedAsync(count, cancellationToken);
        return Ok(scenarios.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ScenarioResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var includeDrafts = User.IsInRole(SystemRoles.Admin) || User.IsInRole(SystemRoles.Trainer);
        var scenario = await _scenarioService.GetByIdAsync(id, includeDrafts, cancellationToken);
        if (scenario is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(scenario));
    }

    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
    public async Task<ActionResult<ScenarioResponse>> Create([FromBody] ScenarioCreateRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var model = new ScenarioUpsertModel
        {
            Title = request.Title,
            Description = request.Description,
            Difficulty = request.Difficulty,
            CorrectOutcome = request.CorrectOutcome,
            MediaAssetId = request.MediaAssetId,
            Tags = request.Tags,
            Notes = request.Notes,
            ExternalReference = request.ExternalReference
        };

        var scenario = await _scenarioService.CreateAsync(model, _currentUser.UserId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = scenario.Id }, ToResponse(scenario));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
    public async Task<ActionResult<ScenarioResponse>> Update(Guid id, [FromBody] ScenarioUpdateRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        var model = new ScenarioUpsertModel
        {
            Title = request.Title,
            Description = request.Description,
            Difficulty = request.Difficulty,
            CorrectOutcome = request.CorrectOutcome,
            MediaAssetId = request.MediaAssetId,
            Tags = request.Tags,
            Notes = request.Notes,
            ExternalReference = request.ExternalReference
        };

        var scenario = await _scenarioService.UpdateAsync(id, model, _currentUser.UserId.Value, cancellationToken);
        return Ok(ToResponse(scenario));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _scenarioService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = SystemRoles.Admin + "," + SystemRoles.Trainer)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        await _scenarioService.PublishAsync(id, _currentUser.UserId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Unauthorized();
        }

        await _scenarioService.ArchiveAsync(id, _currentUser.UserId.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/media-url")]
    [Authorize]
    public async Task<ActionResult<SignedUrlResponse>> GetMediaUrl(Guid id, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioService.GetByIdAsync(id, includeDrafts: true, cancellationToken);
        if (scenario is null)
        {
            return NotFound();
        }

        if (scenario.MediaAsset is null)
        {
            return BadRequest("Scenario media not available");
        }

        var (url, expiresAt) = await _mediaService.GetReadSasUrlAsync(scenario.MediaAsset, cancellationToken);
        return Ok(new SignedUrlResponse
        {
            Url = url,
            ExpiresAt = expiresAt
        });
    }

    private static ScenarioResponse ToResponse(TrainingScenario scenario)
        => new()
        {
            Id = scenario.Id,
            Title = scenario.Title,
            Slug = scenario.Slug,
            Description = scenario.Description,
            Status = scenario.Status,
            Difficulty = scenario.Difficulty,
            CorrectOutcome = scenario.CorrectOutcome,
            MediaAssetId = scenario.MediaAssetId,
            Tags = scenario.Tags.Select(t => t.Tag).ToArray(),
            CreatedAt = scenario.CreatedAt,
            UpdatedAt = scenario.UpdatedAt,
            PublishedAt = scenario.PublishedAt,
            IsArchived = scenario.IsArchived
        };
}