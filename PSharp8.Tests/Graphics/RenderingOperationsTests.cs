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
}