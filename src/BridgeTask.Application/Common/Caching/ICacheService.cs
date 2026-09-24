namespace BridgeTask.Application.Common.Caching;


public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class;

    Task<bool> SetAsync<T>(string key, T value, CancellationToken cancellationToken) where T : class;
}
