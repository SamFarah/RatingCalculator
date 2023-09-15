namespace RcLibrary.Servcies.MemoryCacheServices;

public interface IMemoryCacheService
{
    Task<T?> GetCachedValue<T>(string cacheKey, Func<Task<T>> getter, double ExpiresInSeconds = 3600);
}