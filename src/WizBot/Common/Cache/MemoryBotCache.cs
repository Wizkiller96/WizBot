﻿using Microsoft.Extensions.Caching.Memory;
using OneOf;
using OneOf.Types;

// ReSharper disable InconsistentlySynchronizedField

namespace WizBot.Common;

public sealed class MemoryBotCache : IBotCache
{
    // needed for overwrites and Delete return value
    private readonly object _cacheLock = new object();
    private readonly MemoryCache _cache;

    public MemoryBotCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public ValueTask<bool> AddAsync<T>(TypedKey<T> key, T value, TimeSpan? expiry = null, bool overwrite = true)
    {
        if (overwrite)
        {
            using var item = _cache.CreateEntry(key.Key);
            item.Value = value;
            item.AbsoluteExpirationRelativeToNow = expiry;
            return new(true);
        }
        
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key.Key, out var old) && old is not null)
                return new(false);
            
            using var item = _cache.CreateEntry(key.Key);
            item.Value = value;
            item.AbsoluteExpirationRelativeToNow = expiry;
            return new(true);
        }
    }

    public async ValueTask<T?> GetOrAddAsync<T>(
        TypedKey<T> key,
        Func<Task<T?>> createFactory,
        TimeSpan? expiry = null)
        => await _cache.GetOrCreateAsync(key.Key,
            async ce =>
            {
                ce.AbsoluteExpirationRelativeToNow = expiry;
                var val = await createFactory();
                return val;
            });

    public ValueTask<OneOf<T, None>> GetAsync<T>(TypedKey<T> key)
    {
        if (!_cache.TryGetValue(key.Key, out var val) || val is null)
            return new(new None());

        return new((T)val);
    }

    public ValueTask<bool> RemoveAsync<T>(TypedKey<T> key)
    {
        lock (_cacheLock)
        {
            var toReturn = _cache.TryGetValue(key.Key, out var old ) && old is not null;
            _cache.Remove(key.Key);
            return new(toReturn);
        }
    }
}