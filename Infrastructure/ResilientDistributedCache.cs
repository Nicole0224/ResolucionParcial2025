using Microsoft.Extensions.Caching.Distributed;

namespace GestionCreditos.Infrastructure;

/// <summary>
/// Implementación de <see cref="IDistributedCache"/> que usa Redis como
/// almacenamiento primario y degrada a memoria cuando Redis no está disponible,
/// para no interrumpir el funcionamiento de la aplicación.
/// </summary>
public sealed class ResilientDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _redis;
    private readonly IDistributedCache _memoria;

    public ResilientDistributedCache(IDistributedCache redis, IDistributedCache memoria)
    {
        _redis = redis;
        _memoria = memoria;
    }

    public byte[]? Get(string key)
    {
        try { return _redis.Get(key); }
        catch (Exception) { return _memoria.Get(key); }
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        try { return await _redis.GetAsync(key, token).ConfigureAwait(false); }
        catch (Exception) { return await _memoria.GetAsync(key, token).ConfigureAwait(false); }
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        try { _redis.Set(key, value, options); }
        catch (Exception) { _memoria.Set(key, value, options); }
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        try { await _redis.SetAsync(key, value, options, token).ConfigureAwait(false); }
        catch (Exception) { await _memoria.SetAsync(key, value, options, token).ConfigureAwait(false); }
    }

    public void Refresh(string key)
    {
        try { _redis.Refresh(key); }
        catch (Exception) { _memoria.Refresh(key); }
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        try { await _redis.RefreshAsync(key, token).ConfigureAwait(false); }
        catch (Exception) { await _memoria.RefreshAsync(key, token).ConfigureAwait(false); }
    }

    public void Remove(string key)
    {
        try { _redis.Remove(key); }
        catch (Exception) { _memoria.Remove(key); }
    }

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        try { await _redis.RemoveAsync(key, token).ConfigureAwait(false); }
        catch (Exception) { await _memoria.RemoveAsync(key, token).ConfigureAwait(false); }
    }
}