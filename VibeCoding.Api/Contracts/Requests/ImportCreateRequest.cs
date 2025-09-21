namespace VibeCoding.Api.Contracts.Requests;

public class ImportCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? SourceBlobName { get; set; }
        = null;
    public IList<ImportScenarioRequest> Scenarios { get; set; } = new List<ImportScenarioRequest>();
}