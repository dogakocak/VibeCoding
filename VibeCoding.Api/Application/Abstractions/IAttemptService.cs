using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Application.Abstractions;

public interface IAttemptService
{
    Task<ScenarioAttempt> RecordAttemptAsync(AttemptCreateModel model, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScenarioAttempt>> GetAttemptsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
