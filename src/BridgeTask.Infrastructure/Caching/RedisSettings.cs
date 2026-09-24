namespace BridgeTask.Infrastructure.Caching;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public int DefaultExpirationMinutes { get; set; }
    public int FailureCooldownSeconds { get; set; }
}
