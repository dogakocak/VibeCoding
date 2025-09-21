namespace VibeCoding.Api.Infrastructure.Options;

public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "media";
    public int AssetReadExpiryMinutes { get; set; } = 10;
    public int AssetWriteExpiryMinutes { get; set; } = 10;
    public string? PublicCdnHost { get; set; }
        = null;
}
