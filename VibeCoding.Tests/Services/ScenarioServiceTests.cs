using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VibeCoding.Api.Application.Services;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;
using VibeCoding.Api.Infrastructure.Data;
using Xunit;

namespace VibeCoding.Tests.Services;

public class ScenarioServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsScenarioAndSlug()
    {
        await using var context = CreateContext();
        var service = new ScenarioService(context, NullLogger<ScenarioService>.Instance);
        var creator = Guid.NewGuid();
        var media = new MediaAsset { Id = Guid.NewGuid(), BlobName = "test", ContentType = "image/png" };
        context.MediaAssets.Add(media);
        await context.SaveChangesAsync();

        var model = new ScenarioUpsertModel
        {
            Title = "Suspicious Email",
            Description = "Look for red flags",
            Difficulty = ScenarioDifficulty.Medium,
            CorrectOutcome = ScenarioOutcome.Fake,
            MediaAssetId = media.Id,
            Tags = new List<string> { "email", "phishing" }
        };

        var scenario = await service.CreateAsync(model, creator);

        var stored = await context.TrainingScenarios.Include(s => s.Tags).SingleAsync();
        Assert.Equal(scenario.Id, stored.Id);
        Assert.False(string.IsNullOrWhiteSpace(stored.Slug));
        Assert.Contains(stored.Tags, t => t.Tag == "email");
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTags()
    {
        await using var context = CreateContext();
        var service = new ScenarioService(context, NullLogger<ScenarioService>.Instance);
        var userId = Guid.NewGuid();
        var media = new MediaAsset { Id = Guid.NewGuid(), BlobName = "test", ContentType = "image/png" };
        context.MediaAssets.Add(media);
        var scenario = new TrainingScenario
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Slug = "original",
            Description = "desc",
            MediaAssetId = media.Id,
            CreatedById = userId,
            LastModifiedById = userId,
            Tags = new List<ScenarioTag>
            {
                new() { ScenarioId = media.Id, Tag = "old" }
            }
        };
        context.TrainingScenarios.Add(scenario);
        await context.SaveChangesAsync();

        var model = new ScenarioUpsertModel
        {
            Title = "Original",
            Description = "desc",
            Difficulty = ScenarioDifficulty.Hard,
            CorrectOutcome = ScenarioOutcome.Real,
            MediaAssetId = media.Id,
            Tags = new List<string> { "new" }
        };

        var updated = await service.UpdateAsync(scenario.Id, model, userId);
        await context.Entry(updated).Collection(s => s.Tags).LoadAsync();

        Assert.Single(updated.Tags);
        Assert.Equal("new", updated.Tags.First().Tag);
    }
}
