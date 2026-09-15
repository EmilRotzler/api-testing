using ApiTesting.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ApiTesting.Services;

public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    public T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> factory)
    {
        return cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ttl;
            return factory();
        })!;
    }

    public void Invalidate(string key)
    {
        cache.Remove(key);
    }
}
