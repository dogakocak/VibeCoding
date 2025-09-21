namespace VibeCoding.Api.Contracts.Responses;

public class ImportLogResponse
{
    public required DateTimeOffset LoggedAt { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
}