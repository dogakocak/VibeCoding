namespace VibeCoding.Api.Infrastructure.Options;

public class PostgresOptions
{
    public const string SectionName = "Postgres";

    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveLogging { get; set; } = false;
}
