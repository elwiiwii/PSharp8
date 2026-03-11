namespace PSharp8.Graphics;

public sealed class LruCache<TKey, TValue>
    where TKey : notnull
    where TValue : class, IDisposable
{
    private readonly int _staleTtlFrames;
    private readonly Dictionary<TKey, (TValue value, int lastAccessedFrame)> _entries = [];
    private int _currentFrame;

    public LruCache(int staleTtlFrames = 150)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(staleTtlFrames);
        _staleTtlFrames = staleTtlFrames;
    }

    public int Count => _entries.Count;

    public TValue? Get(TKey key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return null;
        _entries[key] = (entry.value, _currentFrame);
        return entry.value;
    }

    public void Put(TKey key, TValue value)
    {
        if (_entries.TryGetValue(key, out var existing))
            existing.value.Dispose();
        _entries[key] = (value, _currentFrame);
    }

    public void Tick()
    {
        _currentFrame++;
        List<TKey>? toRemove = null;
        foreach (var (key, (value, lastAccessed)) in _entries)
        {
            if (_currentFrame - lastAccessed > _staleTtlFrames)
            {
                (toRemove ??= new()).Add(key);
                value.Dispose();
            }
        }
        if (toRemove is not null)
            foreach (var key in toRemove)
                _entries.Remove(key);
    }

    public void Clear()
    {
        foreach (var (_, (value, _)) in _entries)
            value.Dispose();
        _entries.Clear();
    }
}
