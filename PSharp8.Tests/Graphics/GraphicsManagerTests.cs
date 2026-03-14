using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class GraphicsManagerTests : IDisposable
{
    private static readonly Color Black    = new(0x00, 0x00, 0x00, 255);
    private static readonly Color DarkBlue = new(0x1D, 0x2B, 0x53, 255);
    private static readonly Color Red      = new(0xFF, 0x00, 0x4D, 255);
    private static readonly Color White    = new(0xFF, 0xFF, 0xFF, 255);

    private readonly GraphicsFixture _fixture;
    private readonly GraphicsDevice _gd;
    private readonly List<Texture2D> _ownedTextures = new();

    public GraphicsManagerTests(GraphicsFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _gd = fixture.GraphicsDevice;
    }

    public void Dispose()
    {
        foreach (var t in _ownedTextures)
            if (!t.IsDisposed) t.Dispose();
    }

    // -------------------------------------------------------------------------
    #region Helpers
    // -------------------------------------------------------------------------

    private Texture2D MakeSolid(int width, int height, Color color)
    {
        var tex = new Texture2D(_gd, width, height);
        var data = new Color[width * height];
        Array.Fill(data, color);
        tex.SetData(data);
        _ownedTextures.Add(tex);
        return tex;
    }

    private SpriteMapData BuildSpriteMapData()
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = MakeSolid(8, 8, DarkBlue);
        return new SpriteMapData(sprite, map, "");
    }

    private SpriteTextureManager BuildSpriteTextureManager()
    {
        var pm    = new PaletteManager();
        var smd   = BuildSpriteMapData();
        var cache = new LruCache<SpriteSnapshot, Texture2D>(300);
        return new SpriteTextureManager(_gd, pm, smd, cache);
    }

    /// <summary>
    /// Renders into a <paramref name="width"/> × <paramref name="height"/> render
    /// target, invokes <paramref name="draw"/> inside a SpriteBatch Begin/End
    /// block, then returns the full pixel array for assertion.
    /// </summary>
    private Color[] RenderToTarget(int width, int height, Color clearColor, Action<GraphicsManager> draw,
        (int W, int H)? cellResolution = null)
    {
        (int W, int H) res = cellResolution ?? (width, height);
        using var target = new RenderTarget2D(_gd, width, height);
        var pixel = MakeSolid(1, 1, White);
        using var spriteBatch = new SpriteBatch(_gd);
        var gm = new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            pixel,
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => res);

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

    // -------------------------------------------------------------------------
    #endregion
    #region Constructor — argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenBatchIsNull()
    {
        var act = () => new GraphicsManager(
            null!,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("batch");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            null!,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphics");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGraphicsDeviceIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            null!,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphicsDevice");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPaletteManagerIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            null!,
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("paletteManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPixelIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            null!,
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("pixel");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSpriteTextureManagerIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            null!,
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("spriteTextureManager");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenTextureDictionaryIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            null!,
            _fixture.Window,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("textureDictionary");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenWindowIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            null!,
            () => (128, 128));

        act.Should().Throw<ArgumentNullException>().WithParameterName("window");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGetCellResolutionIsNull()
    {
        using var spriteBatch = new SpriteBatch(_gd);
        var act = () => new GraphicsManager(
            spriteBatch,
            _fixture.GraphicsDeviceManager,
            _gd,
            new PaletteManager(),
            MakeSolid(1, 1, White),
            BuildSpriteTextureManager(),
            new Dictionary<string, Texture2D>(),
            _fixture.Window,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("getCellResolution");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region DrawScaledPixel
    // -------------------------------------------------------------------------

    [Fact]
    public void DrawScaledPixel_PaintsExpectedColor_AtTargetCoordinate()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.DrawScaledPixel(5, 7, Red));

        // pixel at (5, 7) → index = 7 * 20 + 5
        pixels[7 * 20 + 5].Should().Be(Red);
    }

    [Fact]
    public void DrawScaledPixel_FillsRectangle_WhenScaleXAndScaleYAreGreaterThanOne()
    {
        // Draw a 3 wide × 2 tall block at (4, 4) — covers x∈[4,6], y∈[4,5]
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.DrawScaledPixel(4, 4, Red, scaleX: 3, scaleY: 2));

        pixels[4 * 20 + 4].Should().Be(Red);   // top-left corner
        pixels[4 * 20 + 5].Should().Be(Red);   // top-middle
        pixels[4 * 20 + 6].Should().Be(Red);   // top-right corner
        pixels[5 * 20 + 4].Should().Be(Red);   // bottom-left corner
        pixels[5 * 20 + 6].Should().Be(Red);   // bottom-right corner
        pixels[3 * 20 + 4].Should().Be(Black); // one row above — not painted
        pixels[6 * 20 + 4].Should().Be(Black); // one row below — not painted
        pixels[4 * 20 + 7].Should().Be(Black); // one column right — not painted
    }

    [Fact]
    public void DrawScaledPixel_ClampsWidth_ToOnePixel_WhenScaleXIsZero()
    {
        // scaleX = 0 → Math.Max(1, (int)0) = 1; only the single column at x is painted
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.DrawScaledPixel(5, 5, Red, scaleX: 0, scaleY: 1));

        pixels[5 * 20 + 5].Should().Be(Red);  // drawn
        pixels[5 * 20 + 6].Should().Be(Black); // column to the right — not painted
    }

    [Fact]
    public void DrawScaledPixel_ClampsHeight_ToOnePixel_WhenScaleYIsZero()
    {
        // scaleY = 0 → Math.Max(1, (int)0) = 1; only the single row at y is painted
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.DrawScaledPixel(5, 5, Red, scaleX: 1, scaleY: 0));

        pixels[5 * 20 + 5].Should().Be(Red);  // drawn
        pixels[6 * 20 + 5].Should().Be(Black); // row below — not painted
    }

    // --- Viewport scaling ---

    [Fact]
    public void DrawScaledPixel_ScalesCellCoordsToPixelCoords_WhenScaleIs4()
    {
        // 400×400 viewport, (100,100) cell resolution → scale = min(400/100, 400/100) = 4
        // offsetX = (400 - 100*4)/2 = 0, offsetY = 0
        // Cell (5,5) → pixel top-left = (20,20), block = 4×4
        var pixels = RenderToTarget(400, 400, Black,
            gm => gm.DrawScaledPixel(5, 5, Red),
            cellResolution: (100, 100));

        pixels[20 * 400 + 20].Should().Be(Red);  // top-left of 4×4 block
        pixels[20 * 400 + 23].Should().Be(Red);  // top-right of block
        pixels[23 * 400 + 20].Should().Be(Red);  // bottom-left of block
        pixels[19 * 400 + 19].Should().Be(Black); // one pixel before top-left
        pixels[20 * 400 + 24].Should().Be(Black); // one pixel right of block
    }

    [Fact]
    public void DrawScaledPixel_ScalesScaleXAndScaleY_WhenScaleIs4()
    {
        // 400×400 viewport, (100,100) cell resolution → scale = 4
        // Cell (0,0) with scaleX=2, scaleY=3 → pixel block = (2*4)×(3*4) = 8×12
        var pixels = RenderToTarget(400, 400, Black,
            gm => gm.DrawScaledPixel(0, 0, Red, scaleX: 2, scaleY: 3),
            cellResolution: (100, 100));

        pixels[0 * 400 + 7].Should().Be(Red);    // top-right of 8-wide block
        pixels[11 * 400 + 0].Should().Be(Red);   // bottom-left of 12-tall block
        pixels[11 * 400 + 7].Should().Be(Red);   // bottom-right corner
        pixels[0 * 400 + 8].Should().Be(Black);  // one pixel right of block
        pixels[12 * 400 + 0].Should().Be(Black); // one pixel below block
    }

    [Fact]
    public void DrawScaledPixel_UsesFullViewportWidth_WithNoLetterbox()
    {
        // 500×400 viewport, (100,100) cell resolution
        // scaleX = 500/100 = 5, scaleY = 400/100 = 4 — axes scaled independently
        // Cell (0,0) → pixel top-left = (0,0), block = 5 wide × 4 tall
        var pixels = RenderToTarget(500, 400, Black,
            gm => gm.DrawScaledPixel(0, 0, Red),
            cellResolution: (100, 100));

        pixels[0 * 500 + 0].Should().Be(Red);   // starts at left edge — no letterbox
        pixels[0 * 500 + 4].Should().Be(Red);   // last column of 5-wide block
        pixels[0 * 500 + 5].Should().Be(Black); // one pixel past block
        pixels[3 * 500 + 0].Should().Be(Red);   // last row of 4-tall block
        pixels[4 * 500 + 0].Should().Be(Black); // one row past block
    }

    // --- Independent per-axis scaling ---
    // The current uniform-scale + letterbox approach breaks when the system is
    // changed to fit each axis independently to the closest integer multiple of
    // the cell dimension (no letterboxing, non-square "pixels" when aspect
    // ratios differ). The three tests below confirm where the current
    // implementation falls short.

    [Fact]
    public void DrawScaledPixel_StartsAtLeftEdge_WhenHorizontalScaleExceedsVertical()
    {
        // 500×300 viewport, (100,100) cell resolution
        // Independent axes: scaleX = 500/100 = 5, scaleY = 300/100 = 3, offsetX = 0
        // Uniform (current):  scale = min(5,3) = 3, offsetX = (500-300)/2 = 100
        // Cell (0,0) must start painting at pixel x=0 — no horizontal letterbox.
        var pixels = RenderToTarget(500, 300, Black,
            gm => gm.DrawScaledPixel(0, 0, Red),
            cellResolution: (100, 100));

        pixels[0 * 500 + 0].Should().Be(Red); // x=0 is filled — no padding
    }

    [Fact]
    public void DrawScaledPixel_BlockWidthMatchesHorizontalViewportScale_WhenAspectRatioDiffers()
    {
        // 500×300 viewport, (100,100) cell resolution → scaleX = 500/100 = 5
        // Cell-width block should be 5 pixels wide, not 3 (the min-axis scale).
        var pixels = RenderToTarget(500, 300, Black,
            gm => gm.DrawScaledPixel(0, 0, Red),
            cellResolution: (100, 100));

        pixels[0 * 500 + 4].Should().Be(Red);   // last pixel of 5-wide block
        pixels[0 * 500 + 5].Should().Be(Black);  // one pixel past the right edge
    }

    [Fact]
    public void DrawScaledPixel_BlockHeightMatchesVerticalViewportScale_WhenAspectRatioDiffers()
    {
        // 300×500 viewport, (100,100) cell resolution → scaleY = 500/100 = 5 (not min = 3)
        // Cell-height block should be 5 pixels tall, starting at y=0 — no vertical offset.
        var pixels = RenderToTarget(300, 500, Black,
            gm => gm.DrawScaledPixel(0, 0, Red),
            cellResolution: (100, 100));

        pixels[4 * 300 + 0].Should().Be(Red);   // last row of 5-tall block (y=4)
        pixels[5 * 300 + 0].Should().Be(Black);  // one row past the bottom edge (y=5)
    }

    // -------------------------------------------------------------------------
    #endregion
}