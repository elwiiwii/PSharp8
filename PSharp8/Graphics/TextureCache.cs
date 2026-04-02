using Microsoft.Xna.Framework.Graphics;

namespace PSharp8.Graphics;

internal sealed class TextureCache : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _texturesDirectory;
    private readonly LruCache<string, Texture2D> _cache;

    internal TextureCache(GraphicsDevice graphicsDevice, string texturesDirectory, int staleTtlFrames = 150)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _texturesDirectory = texturesDirectory ?? throw new ArgumentNullException(nameof(texturesDirectory));
        _cache = new LruCache<string, Texture2D>(staleTtlFrames); // throws ArgumentOutOfRangeException on negative
    }

    internal Texture2D Get(string name)
    {
        var cached = _cache.Get(name);
        if (cached is not null)
            return cached;

        var path = Path.Combine(_texturesDirectory, name + ".png");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: '{path}'.", path);

        using var stream = File.OpenRead(path);
        var texture = Texture2D.FromStream(_graphicsDevice, stream);
        _cache.Put(name, texture);
        return texture;
    }

    internal void Put(string name, Texture2D texture) => _cache.Put(name, texture);

    internal void Tick() => _cache.Tick();

    public void Dispose() => _cache.Clear();
}
