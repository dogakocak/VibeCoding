namespace VibeCoding.Api.Infrastructure.Options;

public class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool EnableApplicationInsights { get; set; } = false;
    public string? InstrumentationKey { get; set; }
        = null;
    public string UserHashSalt { get; set; } = string.Empty;
}
