using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VibeCoding.Api.Application.Abstractions;
using VibeCoding.Api.Application.Services;
using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;
using VibeCoding.Api.Infrastructure.Data;
using Xunit;

namespace VibeCoding.Tests.Services;

public class AttemptServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RecordAttemptAsync_ComputesScoreAndPersists()
    {
        await using var context = CreateContext();
        var telemetry = new Mock<ITelemetryService>();
        var service = new AttemptService(context, telemetry.Object, NullLogger<AttemptService>.Instance);
        var scenarioId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var media = new MediaAsset { Id = Guid.NewGuid(), BlobName = "blob", ContentType = "image/png" };
        context.MediaAssets.Add(media);
        context.TrainingScenarios.Add(new TrainingScenario
        {
            Id = scenarioId,
            Title = "Test",
            Slug = "test",
            Description = "desc",
            Status = ScenarioStatus.Published,
            CorrectOutcome = ScenarioOutcome.Fake,
            MediaAssetId = media.Id,
            CreatedById = userId,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var request = new AttemptCreateModel
        {
            ScenarioId = scenarioId,
            SelectedOutcome = ScenarioOutcome.Fake,
            ConfidencePercent = 80,
            ResponseTime = TimeSpan.FromSeconds(5),
            SessionId = "session"
        };

        var attempt = await service.RecordAttemptAsync(request, userId);

        var stored = await context.ScenarioAttempts.SingleAsync();
        Assert.Equal(1.0, stored.Score);
        Assert.Equal(userId, stored.UserId);
        telemetry.Verify(t => t.RecordEventAsync(It.Is<TelemetryEventModel>(m => m.EventType == TelemetryEventType.ScenarioAttempted && m.ScenarioId == scenarioId), userId, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
