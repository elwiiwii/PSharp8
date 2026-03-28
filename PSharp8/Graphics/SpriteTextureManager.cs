using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PSharp8.Graphics;

public class SpriteTextureManager : IDisposable
{
    private const int SPRITE_SIZE = 8;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly PaletteManager _paletteManager;
    private readonly SpriteMapData _data;
    private Texture2D? _cachedSpritesheetTexture;
    private int _cachedSpriteVersion = -1;
    private int _cachedPaletteVersion = -1;
    private readonly Dictionary<(int, int, int, int, int), (Texture2D tex, int sprV, int mapV, int palV)> _mapTextureCache = new();
    private readonly LruCache<SpriteSnapshot, Texture2D> _spriteCache;

    public SpriteTextureManager(
        GraphicsDevice graphicsDevice,
        PaletteManager paletteManager,
        SpriteMapData data,
        LruCache<SpriteSnapshot, Texture2D> spriteCache)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _paletteManager = paletteManager ?? throw new ArgumentNullException(nameof(paletteManager));
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _spriteCache = spriteCache ?? throw new ArgumentNullException(nameof(spriteCache));
    }

    public Texture2D GetSpritesheetTexture()
    {
        if (_cachedSpritesheetTexture is null ||
            _data.SpritesheetVersion != _cachedSpriteVersion ||
            _paletteManager.PaletteVersion != _cachedPaletteVersion)
        {
            _cachedSpritesheetTexture ??= new Texture2D(_graphicsDevice, _data.SpriteSheetWidth, _data.SpriteSheetHeight);
            Color[] spritesheetData = _data.SpritesheetData;
            Color[] applied = new Color[spritesheetData.Length];
            for (int i = 0; i < spritesheetData.Length; i++)
                applied[i] = _paletteManager.PaletteMap.TryGetValue(spritesheetData[i], out Color mapped) ? mapped : spritesheetData[i];
            _cachedSpritesheetTexture.SetData(applied);
            _cachedSpriteVersion = _data.SpritesheetVersion;
            _cachedPaletteVersion = _paletteManager.PaletteVersion;
        }
        return _cachedSpritesheetTexture;
    }

    public Texture2D GetMapRegionTexture(int mapX, int mapY, int mapW, int mapH, int flags = 0)
    {
        var key = (mapX, mapY, mapW, mapH, flags);
        int texW = mapW * SPRITE_SIZE;
        int texH = mapH * SPRITE_SIZE;
        int spritesPerRow = _data.SpritesPerRow;
        int spriteSheetWidth = _data.SpriteSheetWidth;
        Color[] spritesheetData = _data.SpritesheetData;

        if (_mapTextureCache.TryGetValue(key, out var cached) &&
            cached.sprV == _data.SpritesheetVersion &&
            cached.mapV == _data.MapVersion &&
            cached.palV == _paletteManager.PaletteVersion)
        {
            return cached.tex;
        }

        Color[] pixels = new Color[texW * texH];
        for (int cy = 0; cy < mapH; cy++)
        {
            for (int cx = 0; cx < mapW; cx++)
            {
                int spriteIndex = _data.GetMapTile(mapX + cx, mapY + cy);
                bool drawSprite = flags == 0 || (_data.GetFlag(spriteIndex) & flags) != 0;
                int spriteOriginX = (spriteIndex % spritesPerRow) * SPRITE_SIZE;
                int spriteOriginY = (spriteIndex / spritesPerRow) * SPRITE_SIZE;
                for (int py = 0; py < SPRITE_SIZE; py++)
                {
                    for (int px = 0; px < SPRITE_SIZE; px++)
                    {
                        int destIdx = (cx * SPRITE_SIZE + px) + (cy * SPRITE_SIZE + py) * texW;
                        if (!drawSprite)
                        {
                            pixels[destIdx] = Color.Transparent;
                            continue;
                        }
                        Color src = spritesheetData[(spriteOriginX + px) + (spriteOriginY + py) * spriteSheetWidth];
                        pixels[destIdx] = _paletteManager.PaletteMap.TryGetValue(src, out Color mapped) ? mapped : src;
                    }
                }
            }
        }

        if (_mapTextureCache.TryGetValue(key, out var existing))
        {
            existing.tex.SetData(pixels);
            _mapTextureCache[key] = (existing.tex, _data.SpritesheetVersion, _data.MapVersion, _paletteManager.PaletteVersion);
            return existing.tex;
        }

        Texture2D tex = new Texture2D(_graphicsDevice, texW, texH);
        tex.SetData(pixels);
        _mapTextureCache[key] = (tex, _data.SpritesheetVersion, _data.MapVersion, _paletteManager.PaletteVersion);
        return tex;
    }

    public Rectangle GetSpriteSourceRect(int spriteIndex, int widthSprites = 1, int heightSprites = 1)
        => _data.GetSpriteSourceRect(spriteIndex, widthSprites, heightSprites);

    public Texture2D GetSpriteTexture(int index, int w = 1, int h = 1)
    {
        int texW = w * SPRITE_SIZE;
        int texH = h * SPRITE_SIZE;
        int spritesPerRow = _data.SpritesPerRow;
        int sheetWidth = _data.SpriteSheetWidth;
        Color[] sheet = _data.SpritesheetData;
        int originX = (index % spritesPerRow) * SPRITE_SIZE;
        int originY = (index / spritesPerRow) * SPRITE_SIZE;

        Color[] pixels = new Color[texW * texH];
        for (int row = 0; row < texH; row++)
            for (int col = 0; col < texW; col++)
                pixels[col + row * texW] = sheet[(originX + col) + (originY + row) * sheetWidth];

        return GetOrCreateCachedTexture(pixels, texW, texH);
    }

    public Texture2D GetScaledRegionTexture(int sx, int sy, int sw, int sh, int dw, int dh)
    {
        int sheetWidth = _data.SpriteSheetWidth;
        Color[] sheet = _data.SpritesheetData;

        Color[] scaled = new Color[dw * dh];
        for (int dy = 0; dy < dh; dy++)
        {
            int srcY = dy * sh / dh;
            for (int dx = 0; dx < dw; dx++)
            {
                int srcX = dx * sw / dw;
                scaled[dx + dy * dw] = sheet[(sx + srcX) + (sy + srcY) * sheetWidth];
            }
        }

        return GetOrCreateCachedTexture(scaled, dw, dh);
    }

    private Texture2D GetOrCreateCachedTexture(Color[] pixels, int width, int height)
    {
        var key = new SpriteSnapshot(pixels, width, height, _paletteManager.PaletteMap);

        Texture2D? cached = _spriteCache.Get(key);
        if (cached is not null)
            return cached;

        var pm = _paletteManager.PaletteMap;
        Color[] applied = new Color[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            applied[i] = pm.TryGetValue(pixels[i], out Color mapped) ? mapped : pixels[i];

        Texture2D tex = new Texture2D(_graphicsDevice, width, height);
        tex.SetData(applied);
        _spriteCache.Put(key, tex);
        return tex;
    }

    public void Tick() => _spriteCache.Tick();

    public void Dispose()
    {
        _cachedSpritesheetTexture?.Dispose();
        foreach (var entry in _mapTextureCache.Values)
            entry.tex.Dispose();
        _mapTextureCache.Clear();
        _spriteCache.Clear();
    }
}
