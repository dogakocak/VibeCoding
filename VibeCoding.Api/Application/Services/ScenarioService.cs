using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;
using VibeCoding.Api.Infrastructure.Data;

namespace VibeCoding.Api.Application.Services;

public class ScenarioService : IScenarioService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ScenarioService> _logger;

    public ScenarioService(ApplicationDbContext dbContext, ILogger<ScenarioService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TrainingScenario> CreateAsync(ScenarioUpsertModel model, Guid userId, CancellationToken cancellationToken = default)
    {
        var scenario = new TrainingScenario
        {
            Id = Guid.NewGuid(),
            Title = model.Title.Trim(),
            Slug = await GenerateUniqueSlugAsync(model.Title, cancellationToken),
            Description = model.Description.Trim(),
            Difficulty = model.Difficulty,
            CorrectOutcome = model.CorrectOutcome,
            MediaAssetId = model.MediaAssetId,
            CreatedById = userId,
            LastModifiedById = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Notes = model.Notes?.Trim(),
            ExternalReference = model.ExternalReference?.Trim()
        };

        scenario.Tags = model.Tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => new ScenarioTag { ScenarioId = scenario.Id, Tag = tag.ToLowerInvariant() })
            .ToList();

        await _dbContext.TrainingScenarios.AddAsync(scenario, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Scenario {ScenarioId} created by {UserId}", scenario.Id, userId);
        return scenario;
    }

    public async Task<TrainingScenario> UpdateAsync(Guid id, ScenarioUpsertModel model, Guid userId, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.TrainingScenarios
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Scenario not found");

        scenario.Title = model.Title.Trim();
        scenario.Description = model.Description.Trim();
        scenario.Difficulty = model.Difficulty;
        scenario.CorrectOutcome = model.CorrectOutcome;
        scenario.MediaAssetId = model.MediaAssetId;
        scenario.Notes = model.Notes?.Trim();
        scenario.ExternalReference = model.ExternalReference?.Trim();
        scenario.LastModifiedById = userId;
        scenario.UpdatedAt = DateTimeOffset.UtcNow;

        _dbContext.ScenarioTags.RemoveRange(scenario.Tags);
        scenario.Tags = model.Tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => new ScenarioTag { ScenarioId = scenario.Id, Tag = tag.ToLowerInvariant() })
            .ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return scenario;
    }

    public async Task<PagedResult<TrainingScenario>> GetPagedAsync(ScenarioQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TrainingScenarios
            .Include(s => s.Tags)
            .Include(s => s.MediaAsset)
            .AsQueryable();

        if (!options.IncludeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        if (options.Status.HasValue)
        {
            query = query.Where(s => s.Status == options.Status.Value);
        }

        if (options.Difficulty.HasValue)
        {
            query = query.Where(s => s.Difficulty == options.Difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            var term = options.Search.Trim().ToLowerInvariant();
            query = query.Where(s => s.Title.ToLower().Contains(term) || s.Description.ToLower().Contains(term));
        }

        query = query.OrderByDescending(s => s.PublishedAt ?? s.CreatedAt);

        var pageSize = Math.Clamp(options.PageSize, 1, 100);
        var page = Math.Max(1, options.Page);
        var skip = (page - 1) * pageSize;

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TrainingScenario>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<TrainingScenario?> GetByIdAsync(Guid id, bool includeDrafts, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TrainingScenarios
            .Include(s => s.Tags)
            .Include(s => s.MediaAsset)
            .AsQueryable();

        if (!includeDrafts)
        {
            query = query.Where(s => s.Status == ScenarioStatus.Published && !s.IsArchived);
        }

        return query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task PublishAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.TrainingScenarios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Scenario not found");

        if (scenario.Status == ScenarioStatus.Published)
        {
            return;
        }

        scenario.Status = ScenarioStatus.Published;
        scenario.PublishedAt = DateTimeOffset.UtcNow;
        scenario.LastModifiedById = userId;
        scenario.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.TrainingScenarios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Scenario not found");

        scenario.IsArchived = true;
        scenario.Status = ScenarioStatus.Archived;
        scenario.LastModifiedById = userId;
        scenario.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.TrainingScenarios.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Scenario not found");

        _dbContext.TrainingScenarios.Remove(scenario);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrainingScenario>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 20);
        return await _dbContext.TrainingScenarios
            .Where(s => s.Status == ScenarioStatus.Published && !s.IsArchived)
            .OrderByDescending(s => s.PublishedAt)
            .ThenByDescending(s => s.CreatedAt)
            .Take(count)
            .Include(s => s.MediaAsset)
            .ToListAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var suffix = 1;

        while (await _dbContext.TrainingScenarios.AnyAsync(s => s.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }

    private static string Slugify(string value)
    {
        var normalized = value.ToLowerInvariant();
        var filtered = new string(normalized.Select(ch => char.IsLetterOrDigit(ch) ? ch : ch == ' ' || ch == '-' ? ch : ' ').ToArray());
        var segments = filtered.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var slug = string.Join('-', segments);
        return string.IsNullOrWhiteSpace(slug) ? $"scenario-{Guid.NewGuid():N}" : slug;
    }
}
