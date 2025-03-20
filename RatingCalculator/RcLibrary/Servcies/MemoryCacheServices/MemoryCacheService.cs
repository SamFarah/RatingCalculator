using Microsoft.Extensions.Caching.Memory;

namespace RcLibrary.Servcies.MemoryCacheServices;
public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetCachedValue<T>(string cacheKey, Func<Task<T>> getter, double expiresInSeconds = 3600, bool checkNull = false)
    {
        if (!_memoryCache.TryGetValue(cacheKey, out T? cachedValue) || (checkNull && cachedValue == null))
        {
            cachedValue = await getter();
            var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(expiresInSeconds));
            _memoryCache.Set(cacheKey, cachedValue, cacheEntryOptions);
        }
        return cachedValue;
    }

    public void RemoveCachedValue(string cacheKey)=> _memoryCache.Remove(cacheKey);    
}
