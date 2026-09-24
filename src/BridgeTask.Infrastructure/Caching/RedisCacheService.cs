using System.Text.Json;
using BridgeTask.Application.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BridgeTask.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _entryOptions;
    private readonly long _failureCooldownMs;
    private readonly ILogger<RedisCacheService> _logger;

    private long _unavailableUntil;

    public RedisCacheService(IDistributedCache cache, IOptions<RedisSettings> settings, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _failureCooldownMs = settings.Value.FailureCooldownSeconds * 1000L;
        _entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(settings.Value.DefaultExpirationMinutes)
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        if (IsInCooldown())
        {
            return null;
        }

        byte[]? payload;

        try
        {
            payload = await _cache.GetAsync(key, cancellationToken);
        }
        catch (Exception ex) when (!IsCancellation(ex, cancellationToken))
        {
            StartCooldown(ex, "read", key);
            return null;
        }

        if (payload is null)
        {
            _logger.LogDebug("Cache miss for {CacheKey}.", key);
            return null;
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(payload, JsonOptions);
            _logger.LogDebug("Cache hit for {CacheKey}.", key);
            return value;
        }
        catch (JsonException ex)
        {
            // Treated as a miss: the value reloaded from the database overwrites the unreadable entry.
            _logger.LogWarning(ex, "Cache entry {CacheKey} could not be read as {CacheType}. Treating it as a miss.", key, typeof(T).Name);
            return null;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, CancellationToken cancellationToken) where T : class
    {
        if (IsInCooldown())
        {
            return false;
        }

        byte[] payload;

        try
        {
            payload = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogError(ex, "Value for {CacheKey} could not be serialized as {CacheType}.", key, typeof(T).Name);
            return false;
        }

        try
        {
            await _cache.SetAsync(key, payload, _entryOptions, cancellationToken);
            return true;
        }
        catch (Exception ex) when (!IsCancellation(ex, cancellationToken))
        {
            StartCooldown(ex, "write", key);
            return false;
        }
    }

    // Without this, every request would wait for the Redis connect timeout while Redis is down.
    private bool IsInCooldown() => Environment.TickCount64 < Interlocked.Read(ref _unavailableUntil);

    private void StartCooldown(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _unavailableUntil, Environment.TickCount64 + _failureCooldownMs);

        _logger.LogWarning(
            ex,
            "Cache {CacheOperation} failed for {CacheKey}. Using the database only for the next {CooldownSeconds}s.",
            operation,
            key,
            _failureCooldownMs / 1000);
    }

    private static bool IsCancellation(Exception ex, CancellationToken cancellationToken)
    {
        return ex is OperationCanceledException && cancellationToken.IsCancellationRequested;
    }
}
