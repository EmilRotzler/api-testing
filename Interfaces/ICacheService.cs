namespace ApiTesting.Interfaces;

public interface ICacheService
{
    T GetOrCreate<T>(string key, TimeSpan ttl, Func<T> factory);

    void Invalidate(string key);
}
