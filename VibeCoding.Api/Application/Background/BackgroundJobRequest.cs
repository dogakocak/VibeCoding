namespace VibeCoding.Api.Application.Background;

public record BackgroundJobRequest(
    BackgroundJobType Type,
    Guid PrimaryId,
    IDictionary<string, string>? Metadata = null
);
