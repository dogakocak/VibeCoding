namespace VibeCoding.Api.Application.Services.Models;

public class ImportRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string SourceBlobName { get; set; } = string.Empty;
    public IList<ImportScenarioDefinition> Scenarios { get; set; } = new List<ImportScenarioDefinition>();
}
