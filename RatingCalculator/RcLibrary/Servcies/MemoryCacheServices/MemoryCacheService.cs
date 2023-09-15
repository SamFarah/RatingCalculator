using Microsoft.Extensions.Caching.Memory;

namespace RcLibrary.Servcies.MemoryCacheServices;
public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetCachedValue<T>(string cacheKey, Func<Task<T>> getter, double ExpiresInSeconds = 3600)
    {
        if (!_memoryCache.TryGetValue(cacheKey, out T? cachedValue))
        {
            cachedValue = await getter();
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(ExpiresInSeconds));
            _memoryCache.Set(cacheKey, cachedValue, cacheEntryOptions);
        }
        return cachedValue;
    }
}
