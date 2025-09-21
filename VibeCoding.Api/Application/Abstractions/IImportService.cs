using VibeCoding.Api.Application.Services.Models;
using VibeCoding.Api.Domain.Entities;

namespace VibeCoding.Api.Application.Abstractions;

public interface IImportService
{
    Task<ImportBatch> CreateImportAsync(ImportRequestModel request, Guid requestedById, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatch>> GetImportsAsync(CancellationToken cancellationToken = default);
    Task QueueImportProcessingAsync(Guid importBatchId, CancellationToken cancellationToken = default);
    Task ProcessImportAsync(Guid importBatchId, CancellationToken cancellationToken = default);
}
