using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;

namespace PSharp8.Tests.Infrastructure;

/// <summary>
/// Shared base for all test classes that require GPU resources.
/// Provides texture lifetime management, common Pico-8 colors, and
/// helpers for building <see cref="GraphicsManager"/> instances and
/// rendering to off-screen targets.
/// </summary>
public abstract class GraphicsTestBase : IDisposable
{
    protected static readonly Color Black     = new(0x00, 0x00, 0x00, 255);
    protected static readonly Color DarkBlue  = new(0x1D, 0x2B, 0x53, 255);
    protected static readonly Color DarkGreen = new(0x00, 0x87, 0x51, 255);
    protected static readonly Color Brown     = new(0xAB, 0x52, 0x36, 255);
    protected static readonly Color White     = new(0xFF, 0xFF, 0xFF, 255);
    protected static readonly Color Red       = new(0xFF, 0x00, 0x4D, 255);

    protected readonly GraphicsFixture _fixture;
    protected readonly GraphicsDevice _gd;
    protected readonly List<Texture2D> _ownedTextures = new();

    protected GraphicsTestBase(GraphicsFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _gd = fixture.GraphicsDevice;
    }

    public void Dispose()
    {
        foreach (var t in _ownedTextures)
            if (!t.IsDisposed) t.Dispose();
    }

    protected Texture2D MakeSolid(int width, int height, Color color)
    {
        var tex = new Texture2D(_gd, width, height);
        var data = new Color[width * height];
        Array.Fill(data, color);
        tex.SetData(data);
        _ownedTextures.Add(tex);
        return tex;
    }

    protected SpriteMapData BuildSpriteMapData()
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = MakeSolid(8, 8, DarkBlue);
        return new SpriteMapData(sprite, map, "");
    }

    protected SpriteTextureManager BuildSpriteTextureManager()
    {
        var pm    = new PaletteManager();
        var smd   = BuildSpriteMapData();
        var cache = new LruCache<SpriteSnapshot, Texture2D>(300);
        return new SpriteTextureManager(_gd, pm, smd, cache);
    }

    /// <summary>
    /// Like <see cref="RenderToTarget"/> but also registers a solid-white texture
    /// for <paramref name="font"/> in the textureDictionary, sized using the unified
    /// cell grid model (cellW × cellH cells, one row per tier).
    /// </summary>
    protected Color[] PrintToTarget(int width, int height, Color clearColor, Font font,
        Action<GraphicsManager> draw, (int W, int H)? cellResolution = null)
    {
        int cellW = font.Characters.Max(pair => pair.Value.Width);
        int cellH = font.Characters.Max(pair => pair.Value.Height);
        int maxCharsPerTier = font.Characters.Max(pair => pair.Key.Length);
        var fontTex = MakeSolid(Math.Max(1, maxCharsPerTier * cellW), Math.Max(1, font.Characters.Count * cellH), White);
        return PrintToTargetCore(width, height, clearColor, fontTex, font.TextureName, draw, cellResolution);
    }

    /// <summary>
    /// Like <see cref="PrintToTarget(int,int,Color,Font,Action{GraphicsManager},(int,int)?)"/> but
    /// accepts an explicit font texture, allowing tests to supply non-uniform or intentionally
    /// wrong-sized textures.
    /// </summary>
    protected Color[] PrintToTarget(int width, int height, Color clearColor, Font font, Texture2D fontTexture,
        Action<GraphicsManager> draw, (int W, int H)? cellResolution = null)
        => PrintToTargetCore(width, height, clearColor, fontTexture, font.TextureName, draw, cellResolution);

    private Color[] PrintToTargetCore(int width, int height, Color clearColor, Texture2D fontTex, string texName,
        Action<GraphicsManager> draw, (int W, int H)? cellResolution = null)
    {
        (int W, int H) res = cellResolution ?? (width, height);
        using var target = new RenderTarget2D(_gd, width, height);
        var pixel = MakeSolid(1, 1, White);
        using var spriteBatch = new SpriteBatch(_gd);
        var texDict = new Dictionary<string, Texture2D> { { texName, fontTex } };
        var gm = new GraphicsManager(
            spriteBatch,
            () => res,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            pixel,
            BuildSpriteTextureManager(),
            texDict,
            _fixture.Window);

        _gd.SetRenderTarget(target);
        _gd.Clear(clearColor);
        spriteBatch.Begin();
        try
        {
            draw(gm);
        }
        catch
        {
            // End the batch and reset the render target before re-throwing so
            // subsequent tests start from a clean graphics-device state.
            spriteBatch.End();
            _gd.SetRenderTarget(null);
            throw;
        }
        spriteBatch.End();
        _gd.SetRenderTarget(null);

        var pixels = new Color[width * height];
        target.GetData(pixels);
        return pixels;
    }

    /// <summary>
    /// Renders into a <paramref name="width"/> × <paramref name="height"/> render
    /// target, invokes <paramref name="draw"/> inside a SpriteBatch Begin/End
    /// block, then returns the full pixel array for assertion.
    /// </summary>
    protected Color[] RenderToTarget(int width, int height, Color clearColor, Action<GraphicsManager> draw,
        (int W, int H)? cellResolution = null)
    {
        (int W, int H) res = cellResolution ?? (width, height);
        using var target = new RenderTarget2D(_gd, width, height);
        var pixel = MakeSolid(1, 1, White);
        using var spriteBatch = new SpriteBatch(_gd);
        var gm = new GraphicsManager(
            spriteBatch,
            () => res,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            pixel,
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window);

        _gd.SetRenderTarget(target);
        _gd.Clear(clearColor);
        spriteBatch.Begin();
        draw(gm);
        spriteBatch.End();
        _gd.SetRenderTarget(null);

        var pixels = new Color[width * height];
        target.GetData(pixels);
        return pixels;
    }
}
