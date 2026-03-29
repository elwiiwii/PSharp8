using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class RenderingOperationsTests(GraphicsFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    #region Helpers
    // -------------------------------------------------------------------------

    private static Font BuildFont_SingleTier(int charW, int charH, string chars)
        => new(new() { { chars, (charW, charH) } }, "TestFont");

    private static Font BuildFont_TwoTiers(
        int w1, int h1, string chars1,
        int w2, int h2, string chars2)
        => new(new() { { chars1, (w1, h1) }, { chars2, (w2, h2) } }, "TestFont");

    private (SpriteTextureManager stm, PaletteManager pm, LruCache<SpriteSnapshot, Texture2D> cache) BuildSpriteSetup(
        int sheetW = 16, int sheetH = 16, Color? fillColor = null,
        Color[]? customPixels = null, string flags = "",
        int staleTtlFrames = 300)
    {
        Color fill = fillColor ?? DarkBlue;
        Texture2D spriteTex;
        if (customPixels is not null)
        {
            spriteTex = new Texture2D(_gd, sheetW, sheetH);
            spriteTex.SetData(customPixels);
            _ownedTextures.Add(spriteTex);
        }
        else
        {
            spriteTex = MakeSolid(sheetW, sheetH, fill);
        }
        var mapTex = MakeSolid(8, 8, fill);
        var smd = new SpriteMapData(spriteTex, mapTex, flags);
        var pm = new PaletteManager();
        var cache = new LruCache<SpriteSnapshot, Texture2D>(staleTtlFrames);
        var stm = new SpriteTextureManager(_gd, pm, smd, cache);
        _ownedDisposables.Add(stm);
        return (stm, pm, cache);
    }

    private (SpriteTextureManager stm, PaletteManager pm, SpriteMapData smd) BuildMapSetup(
        int sheetW = 16, int sheetH = 16,
        int mapW = 16, int mapH = 16,
        Color? fillColor = null,
        string flags = "")
    {
        Color fill = fillColor ?? DarkBlue;
        var spriteTex = MakeSolid(sheetW, sheetH, fill);
        var mapTex = MakeSolid(mapW, mapH, fill);
        var smd = new SpriteMapData(spriteTex, mapTex, flags);
        var pm = new PaletteManager();
        var cache = new LruCache<SpriteSnapshot, Texture2D>(300);
        var stm = new SpriteTextureManager(_gd, pm, smd, cache);
        _ownedDisposables.Add(stm);
        return (stm, pm, smd);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Map
    // -------------------------------------------------------------------------

    [Fact]
    public void Map_DrawsTileAtCorrectPosition()
    {
        var (stm, pm, _) = BuildMapSetup();
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Map(0, 0, 5, 3, 1, 1), pm: pm, stm: stm);

        pixels[3 * 20 + 5].Should().Be(DarkBlue);
        pixels[3 * 20 + 4].Should().Be(Black);
    }

    [Fact]
    public void Map_DrawsMultiTileRegion()
    {
        var (stm, pm, _) = BuildMapSetup();
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Map(0, 0, 0, 0, 2, 2), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
        pixels[15 * 20 + 15].Should().Be(DarkBlue);
        pixels[16 * 20].Should().Be(Black);
    }

    [Fact]
    public void Map_AppliesPaletteMapping()
    {
        var (stm, pm, _) = BuildMapSetup();
        var pixels = RenderToTarget(8, 8, Black, gm =>
        {
            gm.Pal(DarkBlue, Red);
            gm.Map(0, 0, 0, 0, 1, 1);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void Map_AdjustsPosition_ByCameraOffset()
    {
        var (stm, pm, _) = BuildMapSetup();
        var pixels = RenderToTarget(30, 8, Black, gm =>
        {
            gm.Camera(10, 0);
            gm.Map(0, 0, 10, 0, 1, 1);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
        pixels[10].Should().Be(Black);
    }

    [Fact]
    public void Map_DrawsAllTiles_WhenLayersIsZero()
    {
        var (stm, pm, smd) = BuildMapSetup();
        smd.SetFlag(0, 0x1);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Map(0, 0, 0, 0, 1, 1, flags: 0), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Map_FiltersWithLayers()
    {
        var (stm, pm, smd) = BuildMapSetup();
        smd.SetFlag(1, 0x1);
        smd.SetMapTile(0, 0, 1);
        smd.SetMapTile(1, 0, 2);
        var pixels = RenderToTarget(16, 8, Black, gm =>
            gm.Map(0, 0, 0, 0, 2, 1, 0x1), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
        pixels[8].Should().Be(Black);
    }

    [Fact]
    public void Map_ProducesCorrectPixels_OnRepeatCall()
    {
        var (stm, pm, _) = BuildMapSetup();
        var pixels = RenderToTarget(8, 8, Black, gm =>
        {
            gm.Map(0, 0, 0, 0, 1, 1);
            gm.Map(0, 0, 0, 0, 1, 1);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Print
    // -------------------------------------------------------------------------

    [Fact]
    public void Print_RendersCharacterPixels_AtSpecifiedPosition()
    {
        var font = BuildFont_SingleTier(6, 4, "A");
        var pixels = PrintToTarget(20, 20, Black, font, gm =>
            gm.Print("A", 5, 3, Red, font));

        pixels[3 * 20 + 5].Should().Be(Red);
    }

    [Fact]
    public void Print_AdvancesCursorByCharWidth_PerCharacter()
    {
        var font = BuildFont_SingleTier(6, 4, "AB");
        var pixels = PrintToTarget(20, 10, Black, font, gm =>
            gm.Print("AB", 0, 0, Red, font));

        pixels[0].Should().Be(Red); // 'A' starts at x=0
        pixels[6].Should().Be(Red); // 'B' starts at x=6 (one charWidth ahead)
    }

    [Fact]
    public void Print_TintsOutput_WithSpecifiedColor()
    {
        var font = BuildFont_SingleTier(4, 4, "X");
        var pixels = PrintToTarget(10, 10, Black, font, gm =>
            gm.Print("X", 0, 0, DarkBlue, font));

        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Print_AdjustsPosition_ByCameraOffset()
    {
        // Camera(10, 0): Print("A", 10, 0) → logical x = 0, char renders at x=0..5.
        // Without camera correction the char would appear at x=10..15 instead.
        var font = BuildFont_SingleTier(6, 4, "A");
        var pixels = PrintToTarget(30, 4, Black, font, gm =>
        {
            gm.Camera(10, 0);
            gm.Print("A", 10, 0, Red, font);
        });

        pixels[0].Should().Be(Red);  // camera-corrected position
        pixels[10].Should().Be(Black); // uncorrected position must be unpainted
    }

    [Fact]
    public void Print_FallsThroughToSecondTier_WhenCharNotInFirstTier()
    {
        // 'B' is not in tier 1 ("A", charH=4), so Print must search tier 2 ("B", charH=8).
        // The two tiers have different heights so they occupy distinct dictionary keys.
        var font = BuildFont_TwoTiers(6, 4, "A", 6, 8, "B");
        var pixels = PrintToTarget(10, 8, Black, font, gm =>
            gm.Print("B", 0, 0, Red, font));

        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void Print_DoesNotAdvanceCursor_WhenCharacterNotFoundInFont()
    {
        // '?' is not in the font. Per Pico-8 behaviour, the cursor must not advance.
        // 'X' therefore renders at x=0 (not x=6 as it would after a normal advance).
        var font = BuildFont_SingleTier(6, 4, "X");
        var pixels = PrintToTarget(20, 4, Black, font, gm =>
            gm.Print("?X", 0, 0, Red, font));

        pixels[0].Should().Be(Red);  // 'X' at x=0 — cursor not advanced for '?'
        pixels[6].Should().Be(Black); // 'X' is NOT displaced to x=6
    }

    [Fact]
    public void Print_DrawsNothing_WhenFontTextureNotInDictionary()
    {
        // RenderToTarget always passes an empty textureDictionary, so the "TestFont"
        // lookup fails and nothing should be drawn regardless of input.
        var font = BuildFont_SingleTier(6, 4, "A");
        var pixels = RenderToTarget(10, 4, Black, gm =>
            gm.Print("A", 0, 0, Red, font));

        pixels[0].Should().Be(Black);
    }

    [Fact]
    public void Print_DrawsNothing_WhenStringIsEmpty()
    {
        var font = BuildFont_SingleTier(6, 4, "A");
        var pixels = RenderToTarget(10, 4, Black, gm =>
            gm.Print("", 0, 0, Red, font));

        pixels[0].Should().Be(Black);
    }

    [Fact]
    public void Print_Throws_WhenTextureWidthIsNotMultipleOfCellWidth()
    {
        // cellW for a 6-wide char is 6; width 10 is not divisible by 6.
        var font = BuildFont_SingleTier(6, 4, "A");
        var badTex = MakeSolid(10, 4, White);

        var act = () => PrintToTarget(20, 4, Black, font, badTex, gm =>
            gm.Print("A", 0, 0, Red, font));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Print_SamplesFromTopLeftOfCell_WhenCharIsSmallerThanCellSize()
    {
        // Two tiers: 'A' is 4×6, 'B' is 8×6 → cellW=8, cellH=6.
        // Texture: 16×12 (cols=2, 2 tier rows of 8×6 cells).
        // Cell 0 for 'A': top-left 4 columns = White (char region),
        //                 columns 4-7      = DarkGreen (padding — must NOT be drawn).
        // White tint preserves source pixel colors so char vs padding is distinguishable.
        var font = BuildFont_TwoTiers(4, 6, "A", 8, 6, "B");

        var texData = new Color[16 * 12];
        Array.Fill(texData, Black);
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 4; col++) texData[row * 16 + col] = White;     // char 'A'
            for (int col = 4; col < 8; col++) texData[row * 16 + col] = DarkGreen; // padding
        }
        var fontTex = new Texture2D(_gd, 16, 12);
        fontTex.SetData(texData);
        _ownedTextures.Add(fontTex);

        var pixels = PrintToTarget(20, 6, Black, font, fontTex, gm =>
            gm.Print("A", 0, 0, White, font));

        pixels[0].Should().Be(White);  // char 'A' pixel painted from White source
        pixels[4].Should().Be(Black);  // x=4 is outside the 4-wide char — padding not drawn
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Spr
    // -------------------------------------------------------------------------

    [Fact]
    public void Spr_RendersSprite_AtSpecifiedPosition()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Spr(0, 5, 3), pm: pm, stm: stm);

        pixels[3 * 20 + 5].Should().Be(DarkBlue);
        pixels[3 * 20 + 4].Should().Be(Black);
    }

    [Fact]
    public void Spr_AppliesPaletteMapping()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        var pixels = RenderToTarget(8, 8, Black, gm =>
        {
            gm.Pal(DarkBlue, Red);
            gm.Spr(0, 0, 0);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void Spr_RendersBlackPixelsAsTransparent_ByDefaultPalette()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: Black);
        var pixels = RenderToTarget(8, 8, DarkGreen, gm =>
            gm.Spr(0, 0, 0), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkGreen);
    }

    [Fact]
    public void Spr_AdjustsPosition_ByCameraOffset()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        var pixels = RenderToTarget(30, 8, Black, gm =>
        {
            gm.Camera(10, 0);
            gm.Spr(0, 10, 0);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);
        pixels[10].Should().Be(Black);
    }

    [Fact]
    public void Spr_RendersWiderRegion_WhenWidthGreaterThanOne()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        var pixels = RenderToTarget(20, 10, Black, gm =>
            gm.Spr(0, 0, 0, width: 2, height: 1), pm: pm, stm: stm);

        pixels[15].Should().Be(DarkBlue);  // 16px wide, last pixel at x=15
        pixels[16].Should().Be(Black);     // x=16 is outside
    }

    [Fact]
    public void Spr_RendersTallerRegion_WhenHeightGreaterThanOne()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        var pixels = RenderToTarget(10, 20, Black, gm =>
            gm.Spr(0, 0, 0, width: 1, height: 2), pm: pm, stm: stm);

        pixels[15 * 10].Should().Be(DarkBlue);  // 16px tall, last row at y=15
        pixels[16 * 10].Should().Be(Black);      // y=16 is outside
    }

    [Fact]
    public void Spr_FlipsHorizontally_WhenFlipXIsTrue()
    {
        // Red at pixel (0,0), DarkBlue everywhere else
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Spr(0, 0, 0, flipX: true), pm: pm, stm: stm);

        pixels[7].Should().Be(Red);      // flipped: Red moves from x=0 to x=7
        pixels[0].Should().Be(DarkBlue); // x=0 now has DarkBlue
    }

    [Fact]
    public void Spr_FlipsVertically_WhenFlipYIsTrue()
    {
        // Red at pixel (0,0), DarkBlue everywhere else
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Spr(0, 0, 0, flipY: true), pm: pm, stm: stm);

        pixels[7 * 8].Should().Be(Red);  // flipped: Red moves from y=0 to y=7
        pixels[0].Should().Be(DarkBlue); // y=0 now has DarkBlue
    }

    [Fact]
    public void Spr_FlipsBothAxes_WhenBothFlipFlagsTrue()
    {
        // Red at pixel (0,0), DarkBlue everywhere else
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Spr(0, 0, 0, flipX: true, flipY: true), pm: pm, stm: stm);

        pixels[7 * 8 + 7].Should().Be(Red);  // Red moves to (7, 7)
        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Spr_ScalesOutput_WithViewportCellSize()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        // Target 16×16 with cellResolution 8×8 → 2× scale
        var pixels = RenderToTarget(16, 16, Black, gm =>
            gm.Spr(0, 0, 0), pm: pm, stm: stm,
            cellResolution: (8, 8));

        pixels[0].Should().Be(DarkBlue);
        pixels[15 * 16 + 15].Should().Be(DarkBlue); // sprite fills entire 16×16
    }

    [Fact]
    public void Spr_PopulatesCache_AfterFirstCall()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(16, 16, Black, gm =>
            gm.Spr(0, 0, 0), pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Spr_ReusesCache_OnRepeatCall()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(16, 16, Black, gm =>
        {
            gm.Spr(0, 0, 0);
            gm.Spr(0, 0, 0);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Spr_CreatesNewCacheEntry_WhenRelevantPaletteChanges()
    {
        // Sprite is solid DarkBlue; palette swap DarkBlue→Red is relevant
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
        {
            gm.Spr(0, 0, 0);               // cache entry 1 (default palette)
            gm.Pal(DarkBlue, Red);          // relevant palette change
            gm.Spr(0, 0, 0);               // cache entry 2 (new palette)
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Spr_ReusesCacheEntry_WhenIrrelevantPaletteChanges()
    {
        // Sprite is solid DarkBlue; palette swap Brown→Red is irrelevant
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
        {
            gm.Spr(0, 0, 0);               // cache entry 1
            gm.Pal(Brown, Red);             // irrelevant palette change
            gm.Spr(0, 0, 0);               // should reuse entry 1
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Spr_SharesCacheEntry_ForIdenticalSpritesAtDifferentIndices()
    {
        // Solid-fill sheet: sprite 0 and sprite 1 have identical pixel data
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(16, 16, Black, gm =>
        {
            gm.Spr(0, 0, 0);
            gm.Spr(1, 0, 0);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Spr_CreatesSeparateCacheEntry_ForDifferentMultiSpriteSize()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(16, 16, Black, gm =>
        {
            gm.Spr(0, 0, 0, width: 1, height: 1);  // 8×8 sprite
            gm.Spr(0, 0, 0, width: 2, height: 1);  // 16×8 sprite — different size
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Spr_EvictsCacheEntry_AfterTtlExpires()
    {
        var (stm, pm, cache) = BuildSpriteSetup(
            sheetW: 8, sheetH: 8, fillColor: DarkBlue, staleTtlFrames: 3);
        RenderToTarget(8, 8, Black, gm =>
            gm.Spr(0, 0, 0), pm: pm, stm: stm);

        cache.Count.Should().Be(1); // sanity: cached after Spr

        // Tick past the TTL without re-accessing
        for (int i = 0; i < 4; i++)
            stm.Tick();

        cache.Count.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Sspr
    // -------------------------------------------------------------------------

    // --- Core Rendering (no scaling) ---

    [Fact]
    public void Sspr_RendersSourceRegion_AtDestPosition()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 5, 3, 8, 8), pm: pm, stm: stm);

        pixels[3 * 20 + 5].Should().Be(DarkBlue);  // dest at (5, 3)
        pixels[3 * 20 + 4].Should().Be(Black);      // left of dest
    }

    [Fact]
    public void Sspr_SelectsCorrectSubregion_FromSpriteSheet()
    {
        // 16×16 sheet: top-left 8×8 = DarkBlue, bottom-right 8×8 = Red
        var customPixels = new Color[16 * 16];
        Array.Fill(customPixels, DarkBlue);
        for (int row = 8; row < 16; row++)
            for (int col = 8; col < 16; col++)
                customPixels[row * 16 + col] = Red;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 16, sheetH: 16, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(8, 8, 8, 8, 0, 0, 8, 8), pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void Sspr_AppliesPaletteMapping()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        var pixels = RenderToTarget(8, 8, Black, gm =>
        {
            gm.Pal(DarkBlue, Red);
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
    }

    [Fact]
    public void Sspr_RendersBlackPixelsAsTransparent_ByDefaultPalette()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: Black);
        var pixels = RenderToTarget(8, 8, DarkGreen, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkGreen);
    }

    [Fact]
    public void Sspr_AdjustsPosition_ByCameraOffset()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        var pixels = RenderToTarget(30, 8, Black, gm =>
        {
            gm.Camera(10, 0);
            gm.Sspr(0, 0, 8, 8, 10, 0, 8, 8);
        }, pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);   // camera-corrected to x=0
        pixels[10].Should().Be(Black);     // uncorrected position empty
    }

    [Fact]
    public void Sspr_FlipsHorizontally_WhenFlipXIsTrue()
    {
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;  // Red at (0, 0)

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8, flipX: true), pm: pm, stm: stm);

        pixels[7].Should().Be(Red);       // flipped: x=0 → x=7
        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Sspr_FlipsVertically_WhenFlipYIsTrue()
    {
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;  // Red at (0, 0)

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8, flipY: true), pm: pm, stm: stm);

        pixels[7 * 8].Should().Be(Red);   // flipped: y=0 → y=7
        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Sspr_FlipsBothAxes_WhenBothFlipFlagsTrue()
    {
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, DarkBlue);
        customPixels[0] = Red;  // Red at (0, 0)

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8, flipX: true, flipY: true), pm: pm, stm: stm);

        pixels[7 * 8 + 7].Should().Be(Red);  // Red moves to (7, 7)
        pixels[0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Sspr_ScalesOutput_WithViewportCellSize()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        // Target 16×16 with cellResolution 8×8 → 2× viewport scale
        var pixels = RenderToTarget(16, 16, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8), pm: pm, stm: stm,
            cellResolution: (8, 8));

        pixels[0].Should().Be(DarkBlue);
        pixels[15 * 16 + 15].Should().Be(DarkBlue);  // fills entire 16×16
    }

    [Fact]
    public void Sspr_DefaultsDestSizeToSourceSize_WhenSentinelValues()
    {
        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        // destWidth=-1, destHeight=-1 → should default to sw=8, sh=8
        var pixels = RenderToTarget(10, 10, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, -1, -1), pm: pm, stm: stm);

        pixels[0].Should().Be(DarkBlue);       // drawn at dest
        pixels[7].Should().Be(DarkBlue);       // last pixel of 8-wide region
        pixels[8].Should().Be(Black);          // outside dest width
    }

    // --- Pixel-Perfect Scaling ---

    [Fact]
    public void Sspr_DoublesEachPixel_WhenDestIsDoubleSource()
    {
        // 2×2 source: [Red, DarkBlue; DarkGreen, Brown]
        // Scale to 4×4: each pixel → 2×2 block
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0 * 8 + 0] = Red;
        customPixels[0 * 8 + 1] = DarkBlue;
        customPixels[1 * 8 + 0] = DarkGreen;
        customPixels[1 * 8 + 1] = Brown;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(4, 4, Black, gm =>
            gm.Sspr(0, 0, 2, 2, 0, 0, 4, 4), pm: pm, stm: stm);

        // Row 0: [Red, Red, DarkBlue, DarkBlue]
        pixels[0 * 4 + 0].Should().Be(Red);
        pixels[0 * 4 + 1].Should().Be(Red);
        pixels[0 * 4 + 2].Should().Be(DarkBlue);
        pixels[0 * 4 + 3].Should().Be(DarkBlue);
        // Row 1: [Red, Red, DarkBlue, DarkBlue]
        pixels[1 * 4 + 0].Should().Be(Red);
        pixels[1 * 4 + 2].Should().Be(DarkBlue);
        // Row 2: [DarkGreen, DarkGreen, Brown, Brown]
        pixels[2 * 4 + 0].Should().Be(DarkGreen);
        pixels[2 * 4 + 1].Should().Be(DarkGreen);
        pixels[2 * 4 + 2].Should().Be(Brown);
        pixels[2 * 4 + 3].Should().Be(Brown);
        // Row 3: [DarkGreen, DarkGreen, Brown, Brown]
        pixels[3 * 4 + 0].Should().Be(DarkGreen);
        pixels[3 * 4 + 2].Should().Be(Brown);
    }

    [Fact]
    public void Sspr_DistributesPixelsCorrectly_WhenScaling2x2To3x3()
    {
        // 2×2 source: [Red, DarkBlue; DarkGreen, Brown]
        // Pico-8 floor(d*S/D): 3→2 mapping per axis = [0, 0, 1]
        // Expected 3×3:
        //   [Red,       Red,       DarkBlue ]
        //   [Red,       Red,       DarkBlue ]
        //   [DarkGreen, DarkGreen, Brown    ]
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0 * 8 + 0] = Red;
        customPixels[0 * 8 + 1] = DarkBlue;
        customPixels[1 * 8 + 0] = DarkGreen;
        customPixels[1 * 8 + 1] = Brown;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(3, 3, Black, gm =>
            gm.Sspr(0, 0, 2, 2, 0, 0, 3, 3), pm: pm, stm: stm);

        // Row 0: floor(0*2/3)=0, floor(1*2/3)=0, floor(2*2/3)=1
        pixels[0 * 3 + 0].Should().Be(Red);
        pixels[0 * 3 + 1].Should().Be(Red);
        pixels[0 * 3 + 2].Should().Be(DarkBlue);
        // Row 1: same as row 0 (floor(1*2/3)=0 → source row 0)
        pixels[1 * 3 + 0].Should().Be(Red);
        pixels[1 * 3 + 1].Should().Be(Red);
        pixels[1 * 3 + 2].Should().Be(DarkBlue);
        // Row 2: floor(2*2/3)=1 → source row 1
        pixels[2 * 3 + 0].Should().Be(DarkGreen);
        pixels[2 * 3 + 1].Should().Be(DarkGreen);
        pixels[2 * 3 + 2].Should().Be(Brown);
    }

    [Fact]
    public void Sspr_DistributesPixelsCorrectly_WhenScaling3x1To5x1()
    {
        // 3×1 source: [Red, DarkGreen, DarkBlue]
        // Pico-8 floor(d*3/5): [0,0,1,1,2] → [Red, Red, DarkGreen, DarkGreen, DarkBlue]
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0] = Red;
        customPixels[1] = DarkGreen;
        customPixels[2] = DarkBlue;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(5, 1, Black, gm =>
            gm.Sspr(0, 0, 3, 1, 0, 0, 5, 1), pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
        pixels[1].Should().Be(Red);
        pixels[2].Should().Be(DarkGreen);
        pixels[3].Should().Be(DarkGreen);
        pixels[4].Should().Be(DarkBlue);
    }

    [Fact]
    public void Sspr_DownscalesCorrectly_WhenDestSmallerThanSource()
    {
        // 4×4 source with distinct pixels at corners:
        //   (0,0)=Red, (1,0)=DarkBlue, (2,0)=DarkGreen, (3,0)=Brown
        //   (0,2)=Brown, (2,2)=DarkBlue  (others Black)
        // Dest 2×2: floor(d*4/2) → samples [0, 2] per axis
        //   (0,0)→src(0,0)=Red      (1,0)→src(2,0)=DarkGreen
        //   (0,1)→src(0,2)=Brown    (1,1)→src(2,2)=DarkBlue
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0 * 8 + 0] = Red;
        customPixels[0 * 8 + 1] = DarkBlue;
        customPixels[0 * 8 + 2] = DarkGreen;
        customPixels[0 * 8 + 3] = Brown;
        customPixels[2 * 8 + 0] = Brown;
        customPixels[2 * 8 + 2] = DarkBlue;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(2, 2, White, gm =>
            gm.Sspr(0, 0, 4, 4, 0, 0, 2, 2), pm: pm, stm: stm);

        pixels[0 * 2 + 0].Should().Be(Red);
        pixels[0 * 2 + 1].Should().Be(DarkGreen);
        pixels[1 * 2 + 0].Should().Be(Brown);
        pixels[1 * 2 + 1].Should().Be(DarkBlue);
    }

    [Fact]
    public void Sspr_DownscalesCorrectly_WhenNonEvenDivision()
    {
        // 5×1 source: [Red, DarkBlue, DarkGreen, Brown, White]
        // Dest 3×1: floor(d*5/3) → [0, 1, 3]
        // Expected: [Red, DarkBlue, Brown] — position 2 (DarkGreen) skipped, 3 (Brown) kept
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0] = Red;
        customPixels[1] = DarkBlue;
        customPixels[2] = DarkGreen;
        customPixels[3] = Brown;
        customPixels[4] = White;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(3, 1, DarkGreen, gm =>
            gm.Sspr(0, 0, 5, 1, 0, 0, 3, 1), pm: pm, stm: stm);

        pixels[0].Should().Be(Red);
        pixels[1].Should().Be(DarkBlue);
        pixels[2].Should().Be(Brown);
    }

    [Fact]
    public void Sspr_ScalesNonUniformly_WhenDestAspectDiffers()
    {
        // 2×2 source: [Red, DarkBlue; DarkGreen, Brown]
        // Dest 3×2: X maps [0,0,1], Y maps [0,1]
        // Expected 3×2:
        //   [Red,       Red,       DarkBlue]
        //   [DarkGreen, DarkGreen, Brown   ]
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0 * 8 + 0] = Red;
        customPixels[0 * 8 + 1] = DarkBlue;
        customPixels[1 * 8 + 0] = DarkGreen;
        customPixels[1 * 8 + 1] = Brown;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(3, 2, Black, gm =>
            gm.Sspr(0, 0, 2, 2, 0, 0, 3, 2), pm: pm, stm: stm);

        // Row 0
        pixels[0 * 3 + 0].Should().Be(Red);
        pixels[0 * 3 + 1].Should().Be(Red);
        pixels[0 * 3 + 2].Should().Be(DarkBlue);
        // Row 1
        pixels[1 * 3 + 0].Should().Be(DarkGreen);
        pixels[1 * 3 + 1].Should().Be(DarkGreen);
        pixels[1 * 3 + 2].Should().Be(Brown);
    }

    [Fact]
    public void Sspr_FlipsCorrectly_WhenCombinedWithScaling()
    {
        // 2×2 source: [Red, DarkBlue; DarkGreen, Brown]
        // Scale to 3×3 + flipX
        // Unflipped 3×3 (from test 12): [R,R,B; R,R,B; G,G,N]
        // After flipX each row reverses: [B,R,R; B,R,R; N,G,G]
        var customPixels = new Color[8 * 8];
        Array.Fill(customPixels, Black);
        customPixels[0 * 8 + 0] = Red;
        customPixels[0 * 8 + 1] = DarkBlue;
        customPixels[1 * 8 + 0] = DarkGreen;
        customPixels[1 * 8 + 1] = Brown;

        var (stm, pm, _) = BuildSpriteSetup(sheetW: 8, sheetH: 8, customPixels: customPixels);
        var pixels = RenderToTarget(3, 3, Black, gm =>
            gm.Sspr(0, 0, 2, 2, 0, 0, 3, 3, flipX: true), pm: pm, stm: stm);

        // Row 0: [DarkBlue, Red, Red]
        pixels[0 * 3 + 0].Should().Be(DarkBlue);
        pixels[0 * 3 + 1].Should().Be(Red);
        pixels[0 * 3 + 2].Should().Be(Red);
        // Row 2: [Brown, DarkGreen, DarkGreen]
        pixels[2 * 3 + 0].Should().Be(Brown);
        pixels[2 * 3 + 1].Should().Be(DarkGreen);
        pixels[2 * 3 + 2].Should().Be(DarkGreen);
    }

    // --- Cache Interactions ---

    [Fact]
    public void Sspr_PopulatesCache_AfterFirstCall()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8), pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Sspr_ReusesCache_OnRepeatCall()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
        {
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Sspr_CreatesNewCacheEntry_WhenRelevantPaletteChanges()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
        {
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
            gm.Pal(DarkBlue, Red);
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Sspr_ReusesCacheEntry_WhenIrrelevantPaletteChanges()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        RenderToTarget(8, 8, Black, gm =>
        {
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
            gm.Pal(Brown, Red);
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Sspr_SharesCacheEntry_ForIdenticalSourceContent()
    {
        // Solid-fill sheet: source at (0,0) and (8,0) have identical pixels
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 16, sheetH: 16, fillColor: DarkBlue);
        RenderToTarget(16, 16, Black, gm =>
        {
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8);
            gm.Sspr(8, 0, 8, 8, 0, 0, 8, 8);
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Sspr_CreatesSeparateCacheEntry_ForDifferentDestSize()
    {
        var (stm, pm, cache) = BuildSpriteSetup(sheetW: 8, sheetH: 8, fillColor: DarkBlue);
        RenderToTarget(10, 10, Black, gm =>
        {
            gm.Sspr(0, 0, 2, 2, 0, 0, 2, 2);  // 2×2 dest
            gm.Sspr(0, 0, 2, 2, 0, 0, 3, 3);  // 3×3 dest — different size
        }, pm: pm, stm: stm);

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Sspr_EvictsCacheEntry_AfterTtlExpires()
    {
        var (stm, pm, cache) = BuildSpriteSetup(
            sheetW: 8, sheetH: 8, fillColor: DarkBlue, staleTtlFrames: 3);
        RenderToTarget(8, 8, Black, gm =>
            gm.Sspr(0, 0, 8, 8, 0, 0, 8, 8), pm: pm, stm: stm);

        cache.Count.Should().Be(1);

        for (int i = 0; i < 4; i++)
            stm.Tick();

        cache.Count.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
}