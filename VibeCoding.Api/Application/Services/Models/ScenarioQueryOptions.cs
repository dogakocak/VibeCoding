using VibeCoding.Api.Domain.Enums;

namespace VibeCoding.Api.Application.Services.Models;

public class ScenarioQueryOptions
{
    public ScenarioStatus? Status { get; set; }
        = null;
    public ScenarioDifficulty? Difficulty { get; set; }
        = null;
    public string? Search { get; set; }
        = null;
    public bool IncludeArchived { get; set; }
        = false;
    public int Page { get; set; }
        = 1;
    public int PageSize { get; set; }
        = 20;
}
