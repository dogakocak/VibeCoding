using VibeCoding.Api.Application.Services.Models;

namespace VibeCoding.Api.Application.Abstractions;

public interface ITelemetryService
{
    Task RecordEventAsync(TelemetryEventModel model, Guid? userId, string? userEmail, CancellationToken cancellationToken = default);
}
