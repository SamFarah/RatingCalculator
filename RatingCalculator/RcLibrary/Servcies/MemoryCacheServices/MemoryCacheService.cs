using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace RcLibrary.Servcies.MemoryCacheServices;
public class MemoryCacheService : IMemoryCacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
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

    public void RemoveCachedValue(string cacheKey)
    {
        try
        {
            _memoryCache.Remove(cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached value with key {cacheKey}", cacheKey);
        }

    }
}