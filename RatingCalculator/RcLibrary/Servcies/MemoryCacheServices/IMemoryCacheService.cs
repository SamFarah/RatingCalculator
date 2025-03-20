namespace RcLibrary.Servcies.MemoryCacheServices;

public interface IMemoryCacheService
{
    Task<T?> GetCachedValue<T>(string cacheKey, Func<Task<T>> getter, double expiresInSeconds = 3600, bool checkNull = false);
    void RemoveCachedValue(string cacheKey);
}
