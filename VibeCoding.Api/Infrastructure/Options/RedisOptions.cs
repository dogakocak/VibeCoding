namespace VibeCoding.Api.Infrastructure.Options;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = "vibecoding";
    public int DefaultSlidingWindowSeconds { get; set; } = 60;
    public int DefaultPermitLimit { get; set; } = 30;
}
