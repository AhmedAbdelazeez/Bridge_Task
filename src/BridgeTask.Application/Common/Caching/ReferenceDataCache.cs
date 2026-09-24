using Microsoft.Extensions.Logging;

namespace BridgeTask.Application.Common.Caching;

public class ReferenceDataCache
{
    private readonly ICacheService _cache;
    private readonly ILogger<ReferenceDataCache> _logger;

    public ReferenceDataCache(ICacheService cache, ILogger<ReferenceDataCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrLoadAsync<T>(
        CacheKey key,
        Func<CancellationToken, Task<T>> load,
        CancellationToken cancellationToken) where T : class
    {
        var version = await GetVersionAsync(key.Region, cancellationToken);

        if (version is null)
        {
            return await load(cancellationToken);
        }

        var cacheKey = $"{key.Region}:v{version}:{key.Name}";

        var cached = await _cache.GetAsync<T>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await load(cancellationToken);

        await _cache.SetAsync(cacheKey, value, cancellationToken);

        return value;
    }

    // Must be called after the database change is committed, never before.
    public async Task InvalidateAsync(params string[] regions)
    {
        foreach (var region in regions)
        {
            var invalidated = await _cache.SetAsync(VersionKey(region), NewVersion(), CancellationToken.None);

            if (!invalidated)
            {
                _logger.LogError(
                    "Cache invalidation failed for region {CacheRegion}. Cached entries may be stale until they expire.",
                    region);
            }
        }
    }

    // Returns null when the cache cannot be used for this request, so the caller goes straight to the database.
    private async Task<string?> GetVersionAsync(string region, CancellationToken cancellationToken)
    {
        var versionKey = VersionKey(region);

        var version = await _cache.GetAsync<string>(versionKey, cancellationToken);
        if (version is not null)
        {
            return version;
        }

        version = NewVersion();
        return await _cache.SetAsync(versionKey, version, cancellationToken) ? version : null;
    }

    private static string VersionKey(string region) => $"{region}:version";

    private static string NewVersion() => Guid.NewGuid().ToString("N");
}
