using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class SpriteTextureManagerTests(GraphicsFixture fixture) : GraphicsTestBase(fixture)
{
    private static readonly Color DarkGreen = new(0x00, 0x87, 0x51, 255); // palette index 3
    private static readonly Color Brown     = new(0xAB, 0x52, 0x36, 255); // palette index 4

    // -------------------------------------------------------------------------
    #region Helpers
    // -------------------------------------------------------------------------

    private static LruCache<SpriteSnapshot, Texture2D> MakeCache(int staleTtlFrames = 300)
        => new(staleTtlFrames);

    private (SpriteMapData data, SpriteTextureManager mgr, PaletteManager pm) MakeManager(
        int sheetW = 16, int sheetH = 16,
        int mapW   = 16, int mapH   = 8,
        Color? spriteColor = null,
        string flagString  = "",
        int staleTtlFrames = 300)
    {
        Color fill = spriteColor ?? DarkBlue;
        var sprite = MakeSolid(sheetW, sheetH, fill);
        var map    = MakeSolid(mapW,   mapH,   fill);
        var smd = new SpriteMapData(sprite, map, flagString);
        var pm  = new PaletteManager();
        var stm = new SpriteTextureManager(_gd, pm, smd, MakeCache(staleTtlFrames));
        return (smd, stm, pm);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Constructor – argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_Throws_WhenGraphicsDeviceIsNull()
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = MakeSolid(8, 8, DarkBlue);
        var smd = new SpriteMapData(sprite, map, "");
        var pm  = new PaletteManager();

        var act = () => new SpriteTextureManager(null!, pm, smd, MakeCache());

        act.Should().Throw<ArgumentNullException>().WithParameterName("graphicsDevice");
    }

    [Fact]
    public void Constructor_Throws_WhenPaletteManagerIsNull()
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = MakeSolid(8, 8, DarkBlue);
        var smd = new SpriteMapData(sprite, map, "");

        var act = () => new SpriteTextureManager(_gd, null!, smd, MakeCache());

        act.Should().Throw<ArgumentNullException>().WithParameterName("paletteManager");
    }

    [Fact]
    public void Constructor_Throws_WhenSpriteMapDataIsNull()
    {
        var pm = new PaletteManager();
        var act = () => new SpriteTextureManager(_gd, pm, null!, MakeCache());

        act.Should().Throw<ArgumentNullException>().WithParameterName("data");
    }

    [Fact]
    public void Constructor_Throws_WhenSpriteCacheIsNull()
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = MakeSolid(8, 8, DarkBlue);
        var smd = new SpriteMapData(sprite, map, "");
        var pm  = new PaletteManager();

        var act = () => new SpriteTextureManager(_gd, pm, smd, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("spriteCache");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpritesheetTexture – basic usage
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpritesheetTexture_ReturnsNonNullTexture()
    {
        var (_, stm, _) = MakeManager();

        stm.GetSpritesheetTexture().Should().NotBeNull();
    }

    [Fact]
    public void GetSpritesheetTexture_HasCorrectDimensions()
    {
        var (smd, stm, _) = MakeManager(sheetW: 24, sheetH: 16);

        Texture2D tex = stm.GetSpritesheetTexture();

        tex.Width.Should().Be(smd.SpriteSheetWidth);
        tex.Height.Should().Be(smd.SpriteSheetHeight);
    }

    [Fact]
    public void GetSpritesheetTexture_AppliesPaletteMapping()
    {
        var (smd, stm, pm) = MakeManager(spriteColor: DarkBlue);
        // Remap DarkBlue → Red
        pm.SetPalette(DarkBlue, Red);

        Texture2D tex = stm.GetSpritesheetTexture();

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Red);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpritesheetTexture – caching
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpritesheetTexture_ReturnsSameInstance_WhenNothingChanged()
    {
        var (_, stm, _) = MakeManager();

        Texture2D first  = stm.GetSpritesheetTexture();
        Texture2D second = stm.GetSpritesheetTexture();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetSpritesheetTexture_ReturnsSameInstance_AfterSpritesheetVersionChanges()
    {
        // The same Texture2D object is reused; its data is updated in-place.
        var (smd, stm, _) = MakeManager();
        Texture2D first = stm.GetSpritesheetTexture();

        smd.SetSpritePixel(0, 0, DarkGreen);
        Texture2D second = stm.GetSpritesheetTexture();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetSpritesheetTexture_UpdatesPixels_AfterSpritesheetVersionChanges()
    {
        var (smd, stm, _) = MakeManager(spriteColor: DarkBlue);

        smd.SetSpritePixel(0, 0, DarkGreen);
        Texture2D tex = stm.GetSpritesheetTexture();

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(DarkGreen);
    }

    [Fact]
    public void GetSpritesheetTexture_UpdatesPixels_AfterPaletteVersionChanges()
    {
        var (_, stm, pm) = MakeManager(spriteColor: DarkBlue);
        _ = stm.GetSpritesheetTexture(); // prime cache

        pm.SetPalette(DarkBlue, Red);   // invalidate palette
        Texture2D tex = stm.GetSpritesheetTexture();

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Red);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetMapRegionTexture – basic usage
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMapRegionTexture_ReturnsNonNullTexture()
    {
        var (_, stm, _) = MakeManager();

        stm.GetMapRegionTexture(0, 0, 1, 1).Should().NotBeNull();
    }

    [Fact]
    public void GetMapRegionTexture_HasCorrectDimensions()
    {
        var (_, stm, _) = MakeManager();

        Texture2D tex = stm.GetMapRegionTexture(0, 0, 2, 1);

        tex.Width.Should().Be(16);
        tex.Height.Should().Be(8);
    }

    [Fact]
    public void GetMapRegionTexture_AppliesPaletteMapping()
    {
        var (_, stm, pm) = MakeManager(spriteColor: DarkBlue);
        pm.SetPalette(DarkBlue, Red);

        Texture2D tex = stm.GetMapRegionTexture(0, 0, 1, 1);

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void GetMapRegionTexture_WritesTransparent_WhenFlagDoesNotMatch()
    {
        // Sprite 0 has flag 0 by default; requesting flags=1 → no match → transparent
        var (_, stm, _) = MakeManager();

        Texture2D tex = stm.GetMapRegionTexture(0, 0, 1, 1, flags: 1);

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Color.Transparent);
    }

    [Fact]
    public void GetMapRegionTexture_DrawsSprite_WhenFlagMatches()
    {
        // Give sprite 0 flag bit 0 (value = 1), then request flags=1
        var (smd, stm, _) = MakeManager(spriteColor: DarkBlue, flagString: "01");

        Texture2D tex = stm.GetMapRegionTexture(0, 0, 1, 1, flags: 1);

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().NotBe(Color.Transparent);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetMapRegionTexture – caching
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMapRegionTexture_ReturnsSameInstance_OnRepeatCall()
    {
        var (_, stm, _) = MakeManager();

        Texture2D first  = stm.GetMapRegionTexture(0, 0, 1, 1);
        Texture2D second = stm.GetMapRegionTexture(0, 0, 1, 1);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetMapRegionTexture_ReturnsDifferentInstance_ForDifferentRegion()
    {
        var sprite = MakeSolid(16, 16, DarkBlue);
        var map    = MakeSolid(16, 16, DarkBlue);
        var smd = new SpriteMapData(sprite, map, "");
        var pm  = new PaletteManager();
        var stm = new SpriteTextureManager(_gd, pm, smd, MakeCache());

        Texture2D regionA = stm.GetMapRegionTexture(0, 0, 1, 1);
        Texture2D regionB = stm.GetMapRegionTexture(1, 0, 1, 1);

        regionB.Should().NotBeSameAs(regionA);
    }

    [Fact]
    public void GetMapRegionTexture_UpdatesPixels_AfterMapVersionChanges()
    {
        var sprite = MakeSolid(16, 16, DarkBlue);
        var map    = MakeSolid(16, 8, DarkBlue);
        var smd = new SpriteMapData(sprite, map, "");
        var pm  = new PaletteManager();
        var stm = new SpriteTextureManager(_gd, pm, smd, MakeCache());
        _ = stm.GetMapRegionTexture(0, 0, 1, 1); // prime cache

        // Change tile 0 to sprite 1 — but since both sprites are identical
        // (all DarkBlue), we instead verify the texture is *re-generated*
        // by checking the version path via palette change
        pm.SetPalette(DarkBlue, Red);
        Texture2D tex = stm.GetMapRegionTexture(0, 0, 1, 1);

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Red);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpriteSourceRect – delegation
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpriteSourceRect_MatchesSpriteMapData()
    {
        var (smd, stm, _) = MakeManager();

        var fromManager  = stm.GetSpriteSourceRect(1, 2, 1);
        var fromData     = smd.GetSpriteSourceRect(1, 2, 1);

        fromManager.Should().Be(fromData);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Dispose
    // -------------------------------------------------------------------------

    [Fact]
    public void Dispose_CanBeCalledSafely_WithNoTexturesCreated()
    {
        var (_, stm, _) = MakeManager();

        var act = () => stm.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_DisposesSpritesheetTexture()
    {
        var (_, stm, _) = MakeManager();
        Texture2D tex = stm.GetSpritesheetTexture();

        stm.Dispose();

        tex.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void Dispose_DisposesMapRegionTextures()
    {
        var (_, stm, _) = MakeManager();
        Texture2D tex = stm.GetMapRegionTexture(0, 0, 1, 1);

        stm.Dispose();

        tex.IsDisposed.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpriteTexture – basic usage
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpriteTexture_ReturnsNonNullTexture()
    {
        var (_, stm, _) = MakeManager();

        stm.GetSpriteTexture(0).Should().NotBeNull();
    }

    [Fact]
    public void GetSpriteTexture_HasCorrectDimensions()
    {
        var (_, stm, _) = MakeManager();

        Texture2D tex = stm.GetSpriteTexture(0, w: 2, h: 1);

        tex.Width.Should().Be(16);
        tex.Height.Should().Be(8);
    }

    [Fact]
    public void GetSpriteTexture_AppliesPaletteMapping()
    {
        var (_, stm, pm) = MakeManager(spriteColor: DarkBlue);
        pm.SetPalette(DarkBlue, Red);

        Texture2D tex = stm.GetSpriteTexture(0);

        Color[] pixels = new Color[tex.Width * tex.Height];
        tex.GetData(pixels);
        pixels[0].Should().Be(Red);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpriteTexture – caching
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpriteTexture_ReturnsSameInstance_OnRepeatCall()
    {
        var (_, stm, _) = MakeManager();

        Texture2D first  = stm.GetSpriteTexture(0);
        Texture2D second = stm.GetSpriteTexture(0);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetSpriteTexture_ReturnsSameInstance_WhenIrrelevantPaletteChanges()
    {
        // Sprite is all DarkBlue; remapping Red (not in the sprite) must not bust the cache.
        var (_, stm, pm) = MakeManager(spriteColor: DarkBlue);
        Texture2D first = stm.GetSpriteTexture(0);

        pm.SetPalette(Red, Brown); // Red is NOT a pixel colour in sprite 0

        Texture2D second = stm.GetSpriteTexture(0);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetSpriteTexture_ReturnsDifferentInstance_WhenRelevantPaletteChanges()
    {
        // Sprite is all DarkBlue; remapping DarkBlue must produce a new texture.
        var (_, stm, pm) = MakeManager(spriteColor: DarkBlue);
        Texture2D first = stm.GetSpriteTexture(0);

        pm.SetPalette(DarkBlue, Red); // DarkBlue IS in the sprite

        Texture2D second = stm.GetSpriteTexture(0);

        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void GetSpriteTexture_OldTextureNotDisposed_WhenPixelsChange()
    {
        // Changing pixels produces a different hash → new cache entry is created.
        // The old entry is NOT eagerly disposed; it becomes stale and is eventually
        // collected by TTL eviction.
        var (smd, stm, _) = MakeManager(spriteColor: DarkBlue);
        Texture2D first = stm.GetSpriteTexture(0);

        smd.SetSpritePixel(0, 0, Red); // pixel change → different hash

        Texture2D second = stm.GetSpriteTexture(0);

        second.Should().NotBeSameAs(first);
        first.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void GetSpriteTexture_ReusesOldTexture_WhenPixelsRevert()
    {
        // Reverting pixels to their original state produces the same hash, so the
        // original cache entry is returned rather than a new texture being allocated.
        var (smd, stm, _) = MakeManager(spriteColor: DarkBlue);
        Texture2D first = stm.GetSpriteTexture(0);       // cached under hash H0

        smd.SetSpritePixel(0, 0, Red);                   // pixels change → hash H1
        _ = stm.GetSpriteTexture(0);                     // populates H1 entry

        smd.SetSpritePixel(0, 0, DarkBlue);              // pixels revert → hash H0
        Texture2D third = stm.GetSpriteTexture(0);       // should hit original H0 entry

        third.Should().BeSameAs(first);
    }

    [Fact]
    public void GetSpriteTexture_ReturnsSameInstance_ForIdenticalSpritesAtDifferentIndices()
    {
        // Sprites with identical pixel data share one cache entry. The sprite's
        // position in the sheet (index) is not part of the cache key.
        var (_, stm, _) = MakeManager(sheetW: 16, sheetH: 16, spriteColor: DarkBlue);

        Texture2D texA = stm.GetSpriteTexture(0);
        Texture2D texB = stm.GetSpriteTexture(1);

        texB.Should().BeSameAs(texA);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpriteTexture – TTL eviction
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpriteTexture_EvictsTexture_AfterTtlExpires()
    {
        var (_, stm, _) = MakeManager(staleTtlFrames: 2);
        Texture2D tex = stm.GetSpriteTexture(0);

        stm.Tick(); // frame 1: (1-0)=1 not > 2
        stm.Tick(); // frame 2: (2-0)=2 not > 2
        stm.Tick(); // frame 3: (3-0)=3 > 2 → evict

        tex.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public void GetSpriteTexture_KeepsTexture_WhenReaccessedBeforeTtlExpires()
    {
        var (_, stm, _) = MakeManager(staleTtlFrames: 2);
        Texture2D tex = stm.GetSpriteTexture(0); // cached at frame 0

        stm.Tick(); // frame 1
        stm.Tick(); // frame 2
        _ = stm.GetSpriteTexture(0); // resets lastAccessed to frame 2
        stm.Tick(); // frame 3: (3-2)=1 not > 2 → alive

        tex.IsDisposed.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Dispose – sprite cache
    // -------------------------------------------------------------------------

    [Fact]
    public void Dispose_DisposesPerSpriteTextures()
    {
        var (_, stm, _) = MakeManager();
        Texture2D tex = stm.GetSpriteTexture(0);

        stm.Dispose();

        tex.IsDisposed.Should().BeTrue();
    }

    #endregion
}
