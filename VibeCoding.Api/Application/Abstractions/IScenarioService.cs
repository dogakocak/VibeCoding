using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;
using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Application.Abstractions;

public interface IScenarioService
{
    Task<TrainingScenario> CreateAsync(ScenarioUpsertModel model, Guid userId, CancellationToken cancellationToken = default);
    Task<TrainingScenario> UpdateAsync(Guid id, ScenarioUpsertModel model, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<TrainingScenario>> GetPagedAsync(ScenarioQueryOptions options, CancellationToken cancellationToken = default);
    Task<TrainingScenario?> GetByIdAsync(Guid id, bool includeDrafts, CancellationToken cancellationToken = default);
    Task PublishAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingScenario>> GetFeaturedAsync(int count, CancellationToken cancellationToken = default);
}
