using FluentAssertions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PSharp8.Graphics;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class SpriteMapDataTests(GraphicsFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    // Well-known Pico-8 colours, used to paint test textures
    // -------------------------------------------------------------------------
    private static readonly Color DarkGreen = new(0x00, 0x87, 0x51, 255); // palette index 3
    private static readonly Color Brown     = new(0xAB, 0x52, 0x36, 255); // palette index 4

    // -------------------------------------------------------------------------
    #region Helpers
    // -------------------------------------------------------------------------

    // Creates a 2-sprite-wide × 1-sprite-tall spritesheet (16×8):
    //   Sprite 0 (x 0..7) filled with DarkBlue
    //   Sprite 1 (x 8..15) filled with DarkGreen
    private Texture2D MakeTwoSpriteSpritesheet()
    {
        var tex = new Texture2D(_gd, 16, 8);
        var data = new Color[16 * 8];
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 16; x++)
                data[x + y * 16] = x < 8 ? DarkBlue : DarkGreen;
        tex.SetData(data);
        _ownedTextures.Add(tex);
        return tex;
    }

    // Creates a 16×8 map texture (2 cells wide × 1 cell tall):
    //   Cell (0,0) filled with DarkGreen — will match sprite 1
    //   Cell (1,0) filled with DarkBlue  — will match sprite 0
    private Texture2D MakeTwoCellMapTexture()
    {
        var tex = new Texture2D(_gd, 16, 8);
        var data = new Color[16 * 8];
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 16; x++)
                data[x + y * 16] = x < 8 ? DarkGreen : DarkBlue;
        tex.SetData(data);
        _ownedTextures.Add(tex);
        return tex;
    }

    // Minimal valid data: 16×16 spritesheet (4 sprites), 16×8 map (2 cells)
    // Textures are tracked in _ownedTextures so Reload() remains safe
    // throughout the test lifetime.
    private SpriteMapData MakeDefault(string flagString = "")
    {
        var sprite = MakeSolid(16, 16, DarkBlue);
        var map    = MakeSolid(16, 8, DarkBlue);
        return new SpriteMapData(sprite, map, flagString);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Constructor – argument validation
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(7, 8)]   // width not a multiple of 8
    [InlineData(8, 7)]   // height not a multiple of 8
    [InlineData(9, 8)]
    public void Constructor_Throws_WhenSpritesheetDimensionsNotMultipleOf8(int w, int h)
    {
        var sprite = new Texture2D(_gd, w, h);
        _ownedTextures.Add(sprite);
        var map    = MakeSolid(8, 8, DarkBlue);

        var act = () => new SpriteMapData(sprite, map, "");

        act.Should().Throw<ArgumentException>().WithParameterName("spriteTexture");
    }

    [Theory]
    [InlineData(7, 8)]
    [InlineData(8, 7)]
    [InlineData(9, 8)]
    public void Constructor_Throws_WhenMapTextureDimensionsNotMultipleOf8(int w, int h)
    {
        var sprite = MakeSolid(8, 8, DarkBlue);
        var map    = new Texture2D(_gd, w, h);
        _ownedTextures.Add(map);

        var act = () => new SpriteMapData(sprite, map, "");

        act.Should().Throw<ArgumentException>().WithParameterName("mapTexture");
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Constructor – dimensions and initial versions
    // -------------------------------------------------------------------------

    [Fact]
    public void Dimensions_AreCalculatedCorrectly()
    {
        var sprite = MakeSolid(24, 16, DarkBlue); // 3×2 sprites
        var map    = MakeSolid(40, 24, DarkBlue); // 5×3 cells

        var data = new SpriteMapData(sprite, map, "");

        data.SpriteSheetWidth.Should().Be(24);
        data.SpriteSheetHeight.Should().Be(16);
        data.SpritesPerRow.Should().Be(3);
        data.SpriteCount.Should().Be(6);
        data.MapWidth.Should().Be(5);
        data.MapHeight.Should().Be(3);
    }

    [Fact]
    public void Versions_ArePositive_AfterConstruction()
    {
        var data = MakeDefault();

        data.SpritesheetVersion.Should().BeGreaterThan(0);
        data.MapVersion.Should().BeGreaterThan(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Flag loading from flag string
    // -------------------------------------------------------------------------

    [Fact]
    public void GetFlag_ReturnsCorrectValue_FromFlagString()
    {
        // "0105" → sprite 0 = 0x01, sprite 1 = 0x05
        var data = MakeDefault("0105");

        data.GetFlag(0).Should().Be(0x01);
        data.GetFlag(1).Should().Be(0x05);
    }

    [Fact]
    public void GetFlag_ReturnsZero_ForSpritesWithNoFlagInString()
    {
        var data = MakeDefault("01"); // only one flag provided, others default to 0

        data.GetFlag(1).Should().Be(0);
        data.GetFlag(3).Should().Be(0);
    }

    [Fact]
    public void GetFlag_ReturnsZero_ForOutOfRangeSprite()
    {
        var data = MakeDefault();

        data.GetFlag(-1).Should().Be(0);
        data.GetFlag(data.SpriteCount).Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetFlag(n, bit) / SetFlag(n, bit, bool)
    // -------------------------------------------------------------------------

    [Fact]
    public void GetFlagBit_ReturnsFalse_ForOutOfRangeSprite()
    {
        var data = MakeDefault();

        data.GetFlag(-1, 0).Should().BeFalse();
        data.GetFlag(data.SpriteCount, 0).Should().BeFalse();
    }

    [Fact]
    public void GetFlagBit_ReturnsFalse_ForOutOfRangeBit()
    {
        var data = MakeDefault("FF");

        data.GetFlag(0, -1).Should().BeFalse();
        data.GetFlag(0, 8).Should().BeFalse();
    }

    [Fact]
    public void SetFlagBit_SetsIndividualBit()
    {
        var data = MakeDefault();

        data.SetFlag(0, 3, true);

        data.GetFlag(0, 3).Should().BeTrue();
        data.GetFlag(0, 0).Should().BeFalse();
    }

    [Fact]
    public void SetFlagBit_ClearsBit_WhenFalse()
    {
        var data = MakeDefault("FF");

        data.SetFlag(0, 5, false);

        data.GetFlag(0, 5).Should().BeFalse();
        data.GetFlag(0, 7).Should().BeTrue(); // other bits unaffected
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetFlag(n, value)
    // -------------------------------------------------------------------------

    [Fact]
    public void SetFlag_MasksTo8Bits()
    {
        var data = MakeDefault();

        data.SetFlag(0, 0x1FF); // 9-bit value

        data.GetFlag(0).Should().Be(0xFF);
    }

    [Fact]
    public void SetFlag_IsNoOp_ForOutOfRangeSprite()
    {
        var data = MakeDefault();

        var act = () => {
            data.SetFlag(-1, 0xFF);
            data.SetFlag(data.SpriteCount, 0xFF);
        };

        act.Should().NotThrow();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Sprite pixel operations
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpritePixel_ReturnsLoadedColor()
    {
        var data = MakeDefault();
        // The spritesheet was filled with DarkBlue, so every pixel should be DarkBlue
        data.GetSpritePixel(0, 0).Should().Be(DarkBlue);
    }

    [Fact]
    public void GetSpritePixel_ReturnsBlack_ForOutOfBoundsCoordinates()
    {
        var data = MakeDefault();

        data.GetSpritePixel(-1, 0).Should().Be(Color.Black);
        data.GetSpritePixel(0, -1).Should().Be(Color.Black);
        data.GetSpritePixel(data.SpriteSheetWidth, 0).Should().Be(Color.Black);
        data.GetSpritePixel(0, data.SpriteSheetHeight).Should().Be(Color.Black);
    }

    [Fact]
    public void SetSpritePixel_UpdatesStoredColor()
    {
        var data = MakeDefault();

        data.SetSpritePixel(3, 3, Brown);

        data.GetSpritePixel(3, 3).Should().Be(Brown);
    }

    [Fact]
    public void SetSpritePixel_IncrementsSpritesheetVersion()
    {
        var data = MakeDefault();
        int before = data.SpritesheetVersion;

        data.SetSpritePixel(0, 0, Brown);

        data.SpritesheetVersion.Should().Be(before + 1);
    }

    [Fact]
    public void SetSpritePixel_IsNoOp_WhenOutOfBounds()
    {
        var data = MakeDefault();
        int vBefore = data.SpritesheetVersion;

        data.SetSpritePixel(-1, 0, Brown);
        data.SetSpritePixel(data.SpriteSheetWidth, 0, Brown);

        data.SpritesheetVersion.Should().Be(vBefore);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Map tile operations
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMapTile_ReturnsZero_ForOutOfBounds()
    {
        var data = MakeDefault();

        data.GetMapTile(-1, 0).Should().Be(0);
        data.GetMapTile(0, -1).Should().Be(0);
        data.GetMapTile(data.MapWidth, 0).Should().Be(0);
        data.GetMapTile(0, data.MapHeight).Should().Be(0);
    }

    [Fact]
    public void SetMapTile_UpdatesTile_AndIncrementsMapVersion()
    {
        var data = MakeDefault();
        int before = data.MapVersion;

        data.SetMapTile(0, 0, 2);

        data.GetMapTile(0, 0).Should().Be(2);
        data.MapVersion.Should().Be(before + 1);
    }

    [Fact]
    public void SetMapTile_IsNoOp_ForOutOfBoundsCoordinates()
    {
        var data = MakeDefault();
        int before = data.MapVersion;

        data.SetMapTile(-1, 0, 1);
        data.SetMapTile(0, data.MapHeight, 1);

        data.MapVersion.Should().Be(before);
    }

    [Fact]
    public void SetMapTile_IsNoOp_ForOutOfRangeSpriteNumber()
    {
        var data = MakeDefault();
        int before = data.MapVersion;

        data.SetMapTile(0, 0, -1);
        data.SetMapTile(0, 0, data.SpriteCount);

        data.MapVersion.Should().Be(before);
        data.GetMapTile(0, 0).Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Reload
    // -------------------------------------------------------------------------

    [Fact]
    public void Reload_ResolvesMapTilesFromTextures()
    {
        // Sprite 0 = DarkBlue, Sprite 1 = DarkGreen
        // Map cell (0,0) = DarkGreen → should resolve to sprite 1
        // Map cell (1,0) = DarkBlue  → should resolve to sprite 0
        var sprite = MakeTwoSpriteSpritesheet();
        var map    = MakeTwoCellMapTexture();

        var data = new SpriteMapData(sprite, map, "");

        data.GetMapTile(0, 0).Should().Be(1);
        data.GetMapTile(1, 0).Should().Be(0);
    }

    [Fact]
    public void Reload_RestoresOriginalFlags()
    {
        var data = MakeDefault("0A");
        data.SetFlag(0, 0xFF); // mutate flag

        data.Reload();

        data.GetFlag(0).Should().Be(0x0A);
    }

    [Fact]
    public void Reload_IncrementsVersions()
    {
        var data = MakeDefault();
        int sprV = data.SpritesheetVersion;
        int mapV = data.MapVersion;

        data.Reload();

        data.SpritesheetVersion.Should().Be(sprV + 1);
        data.MapVersion.Should().Be(mapV + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region MapToSpritesheet1D
    // -------------------------------------------------------------------------

    [Fact]
    public void MapToSpritesheet1D_WritesToSpritesheetData()
    {
        var data = MakeDefault();
        // MakeDefault gives a 16×16 spritesheet → 4 sprites (indices 0–3).
        // Set tile 0,0 to sprite 3 → high nibble = 3/16 = 0, low nibble = 3%16 = 3
        data.SetMapTile(0, 0, 3);
        int spritesheetVBefore = data.SpritesheetVersion;

        // Encode 2 pixels from cell (0,0) into spritesheet at (0,0)
        data.MapToSpritesheet1D(cellX: 0, cellY: 0, destX: 0, destY: 0, length: 2, @base: 16);

        data.SpritesheetVersion.Should().BeGreaterThan(spritesheetVBefore);
        // Pixel 0 (even offset): 3/16=0  → palette[0]
        // Pixel 1 (odd offset):  3%16=3  → palette[3]
        Color pal0 = Pico8.Palette.ElementAt(0).Key;
        Color pal3 = Pico8.Palette.ElementAt(3).Key;
        data.GetSpritePixel(0, 0).Should().Be(pal0);
        data.GetSpritePixel(1, 0).Should().Be(pal3);
    }

    [Fact]
    public void MapToSpritesheet1D_IsNoOp_WhenLengthIsZero()
    {
        var data = MakeDefault();
        int vBefore = data.SpritesheetVersion;

        data.MapToSpritesheet1D(length: 0);

        data.SpritesheetVersion.Should().Be(vBefore);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region MapToSpritesheet2D
    // -------------------------------------------------------------------------

    [Fact]
    public void MapToSpritesheet2D_WritesToSpritesheetData()
    {
        // 128×128 spritesheet → 256 sprites; 128×8 map → 16×1 = 16 cells (mapHeight = 1)
        var sprite = MakeSolid(128, 128, DarkBlue);
        var map    = MakeSolid(128, 8, DarkBlue);
        var data = new SpriteMapData(sprite, map, "");
        // cellY=0 is the only valid map row; tile 3 is within the 256-sprite range
        data.SetMapTile(0, 0, 3); // high nibble 3/16=0, low nibble 3%16=3
        int vBefore = data.SpritesheetVersion;

        data.MapToSpritesheet2D(
            cellX: 0, cellY: 0, cellW: 1, cellH: 1,
            destX: 0, destY: 64, destW: 2, destH: 1,
            @base: 16);

        data.SpritesheetVersion.Should().BeGreaterThan(vBefore);
        Color pal0 = Pico8.Palette.ElementAt(0).Key;
        Color pal3 = Pico8.Palette.ElementAt(3).Key;
        data.GetSpritePixel(0, 64).Should().Be(pal0);
        data.GetSpritePixel(1, 64).Should().Be(pal3);
    }

    [Fact]
    public void MapToSpritesheet2D_IsNoOp_WhenCellWidthIsZero()
    {
        var data = MakeDefault();
        int vBefore = data.SpritesheetVersion;

        data.MapToSpritesheet2D(cellW: 0);

        data.SpritesheetVersion.Should().Be(vBefore);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region GetSpriteSourceRect
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSpriteSourceRect_ReturnsCorrectRectForSpriteZero()
    {
        var data = MakeDefault();

        var rect = data.GetSpriteSourceRect(0);

        rect.X.Should().Be(0);
        rect.Y.Should().Be(0);
        rect.Width.Should().Be(8);
        rect.Height.Should().Be(8);
    }

    [Fact]
    public void GetSpriteSourceRect_ReturnsCorrectRectForSpriteOnRow1()
    {
        // 16×16 sheet → 2 sprites per row → sprite 2 is at (0,8)
        var data = MakeDefault();
        int spritesPerRow = data.SpritesPerRow; // should be 2

        var rect = data.GetSpriteSourceRect(spritesPerRow);

        rect.X.Should().Be(0);
        rect.Y.Should().Be(8);
    }

    [Fact]
    public void GetSpriteSourceRect_ScalesWithWidthAndHeightSprites()
    {
        var data = MakeDefault();

        var rect = data.GetSpriteSourceRect(0, widthSprites: 2, heightSprites: 3);

        rect.Width.Should().Be(16);
        rect.Height.Should().Be(24);
    }

    #endregion
}
