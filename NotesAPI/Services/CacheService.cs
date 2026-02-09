using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using NotesAPI.Services.Interfaces;

namespace NotesAPI.Services;

public class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await cache.GetStringAsync(key);
        return json == null ? default : JsonSerializer.Deserialize<T>(json, _options);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value, _options);
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue) options.AbsoluteExpirationRelativeToNow = expiry;
        await cache.SetStringAsync(key, json, options);
    }

    public async Task RemoveAsync(string key)
    {
        await cache.RemoveAsync(key);
    }
}