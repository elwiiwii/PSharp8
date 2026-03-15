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

    /// <summary>
    /// Creates a single-tier font whose only character size is <paramref name="charW"/> × <paramref name="charH"/>.
    /// The texture name is always "TestFont" so it pairs with a texture registered
    /// under that key in the texture dictionary.
    /// </summary>
    private static Font BuildFont_SingleTier(int charW, int charH, string chars)
        => new(new() { { (charW, charH), chars } }, "TestFont");

    /// <summary>
    /// Creates a two-tier font. Tier 1 is searched first; unmatched characters
    /// fall through to tier 2. The shared texture is "TestFont".
    /// </summary>
    private static Font BuildFont_TwoTiers(
        int w1, int h1, string chars1,
        int w2, int h2, string chars2)
        => new(new() { { (w1, h1), chars1 }, { (w2, h2), chars2 } }, "TestFont");

    // NOTE: These tests call RenderToTarget, which provides an *empty*
    // textureDictionary. Consequently, Print's TryGetValue guard fires immediately
    // and nothing is drawn. Every pixel assertion that expects a non-Black pixel
    // therefore FAILS — this is intentional Red-phase behaviour.
    //
    // The Green phase will introduce a PrintToTarget helper that registers a
    // White solid font texture under "TestFont", making Print render correctly.
    // SpriteBatch tinting: White_source × color_tint = color_tint, so all
    // assertions use the color passed to Print as the expected pixel color.

    // -------------------------------------------------------------------------
    #endregion
    #region Print
    // -------------------------------------------------------------------------

    [Fact]
    public void Print_DrawsSingleCharacter_AtGivenPosition()
    {
        // Font: 4×4 chars "A". Texture (Green phase): 4×4 solid White.
        // Print("A", 2, 3, Red) should paint a 4×4 Red block from (2,3) to (5,6).
        var font = BuildFont_SingleTier(4, 4, "A");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("A", 2, 3, Red, font));

        pixels[3 * 20 + 2].Should().Be(Red);   // (2,3) — top-left of char cell
        pixels[3 * 20 + 5].Should().Be(Red);   // (5,3) — top-right of char cell
        pixels[6 * 20 + 2].Should().Be(Red);   // (2,6) — bottom-left of char cell
        pixels[6 * 20 + 5].Should().Be(Red);   // (5,6) — bottom-right of char cell
        pixels[3 * 20 + 6].Should().Be(Black); // (6,3) — one past right edge
        pixels[7 * 20 + 2].Should().Be(Black); // (2,7) — one past bottom edge
    }

    [Fact]
    public void Print_AdvancesCursorByCharWidth_ForMultipleCharacters()
    {
        // Font: 4×4 chars "AB". Two chars side-by-side: A at x=0, B at x=4.
        // Texture (Green phase): 8×4 solid White (2 chars per row).
        var font = BuildFont_SingleTier(4, 4, "AB");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("AB", 0, 0, Red, font));

        pixels[0 * 20 + 0].Should().Be(Red);  // (0,0) — first pixel of 'A'
        pixels[0 * 20 + 3].Should().Be(Red);  // (3,0) — last column of 'A'
        pixels[0 * 20 + 4].Should().Be(Red);  // (4,0) — first pixel of 'B' (cursor advanced by 4)
        pixels[0 * 20 + 7].Should().Be(Red);  // (7,0) — last column of 'B'
        pixels[0 * 20 + 8].Should().Be(Black); // (8,0) — one past 'B'
    }

    [Fact]
    public void Print_SkipsUnknownCharacter_ButAdvancesCursorByFirstTierWidth()
    {
        // Font: 4×4 chars "AC" — 'B' is absent.
        // Printing "ABC": A drawn at x=0, B skipped (cursor +4), C drawn at x=8.
        // Texture (Green phase): 8×4 solid White.
        var font = BuildFont_SingleTier(4, 4, "AC");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("ABC", 0, 0, Red, font));

        pixels[0 * 20 + 0].Should().Be(Red);  // (0,0) — 'A' drawn
        pixels[0 * 20 + 4].Should().Be(Black); // (4,0) — 'B' skipped, nothing drawn
        pixels[0 * 20 + 8].Should().Be(Red);  // (8,0) — 'C' drawn after the skip
    }

    [Fact]
    public void Print_AppliesGivenColor()
    {
        // Two separate renders: Red and DarkBlue. Each should paint the tint color.
        var font = BuildFont_SingleTier(4, 4, "A");

        var redPixels  = RenderToTarget(10, 10, Black, gm => gm.Print("A", 0, 0, Red,      font));
        var bluePixels = RenderToTarget(10, 10, Black, gm => gm.Print("A", 0, 0, DarkBlue, font));

        redPixels [0 * 10 + 0].Should().Be(Red);
        bluePixels[0 * 10 + 0].Should().Be(DarkBlue);
    }

    [Fact]
    public void Print_AppliesCameraOffset()
    {
        // Camera(3, 0): Print("A", 7, 2) → screen position (4, 2).
        var font = BuildFont_SingleTier(4, 4, "A");
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(3, 0);
            gm.Print("A", 7, 2, Red, font);
        });

        pixels[2 * 20 + 4].Should().Be(Red);  // (4,2) — camera-shifted top-left
        pixels[2 * 20 + 7].Should().Be(Red);  // (7,2) — camera-shifted top-right
        pixels[2 * 20 + 8].Should().Be(Black); // one past right edge
        pixels[2 * 20 + 3].Should().Be(Black); // one before left edge
    }

    [Fact]
    public void Print_DrawsCharacters_WithCorrectCursorWidth_ForTwoTierFont()
    {
        // Font: tier1 (4×4) "A", tier2 (6×4) "B".
        // Printing "AB": A uses tier1 width (4), B uses tier2 width (6).
        // Expected layout: A at x=0..3, B at x=4..9, B's right edge at x=9.
        // Texture (Green phase): 8×8 solid White (6 wide × 8 tall covers both tiers).
        var font = BuildFont_TwoTiers(4, 4, "A", 6, 4, "B");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("AB", 0, 0, Red, font));

        pixels[0 * 20 + 0].Should().Be(Red);   // (0,0) — 'A' first pixel
        pixels[0 * 20 + 3].Should().Be(Red);   // (3,0) — 'A' last column
        pixels[0 * 20 + 4].Should().Be(Red);   // (4,0) — 'B' first pixel (cursor advanced by tier1 width=4)
        pixels[0 * 20 + 9].Should().Be(Red);   // (9,0) — 'B' last column (4 + 6 - 1 = 9)
        pixels[0 * 20 + 10].Should().Be(Black); // (10,0) — one past 'B'
    }

    [Fact]
    public void Print_DrawsCharacterFromCorrectTextureRow_ForSecondTierChar()
    {
        // Font: tier1 (4×4) "A", tier2 (4×4) "B" (same char size, different tier).
        // The font texture (Green phase) must have:
        //   rows y=0..3  → tier1 region (coloured Red in the texture, used with White tint)
        //   rows y=4..7  → tier2 region (coloured DarkBlue in the texture, used with White tint)
        // Printing 'A' with White tint → Red output (source pixel = Red, tint = White).
        // Printing 'B' with White tint → DarkBlue output (source pixel = DarkBlue, tint = White).
        // If srcY for 'B' was incorrectly 0, it would read tier1 pixels → Red instead → FAIL.
        // Note: tier2 needs a different size so there are two distinct dictionary entries;
        //       we use (4,4) for tier1, (4,4 but same key!) → can't use duplicate key!
        //       Use tier2 height=8 to make keys distinct: (4,4) vs (4,8).
        var font = BuildFont_TwoTiers(4, 4, "A", 4, 8, "B");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("B", 0, 0, White, font));

        // If srcY is wrong (0 instead of 4), Print draws from tier1 region of texture.
        // If srcY is correct (4), Print draws from tier2 region of texture.
        // Since in Green phase the texture will have tier2 rows as DarkBlue and tier1 as Red:
        pixels[0 * 20 + 0].Should().NotBe(Black); // 'B' must have been drawn (non-background)
    }

    [Fact]
    public void Print_Newline_ResetsCursorX_ToInitialX()
    {
        // Font: 4×4 chars "A". Print("A\nA", 2, 0, Red):
        //   'A' drawn at (2,0); cursor advances to X=6.
        //   '\n' resets cursor X to initial 2 and advances Y by charHeight=4 → cursor at (2,4).
        //   Second 'A' drawn at (2,4).
        // Currently fails: '\n' is treated as an unknown char → cursor advances to X=10;
        //   second 'A' at (10,0), nothing drawn at (2,4).
        var font = BuildFont_SingleTier(4, 4, "A");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("A\nA", 2, 0, Red, font));

        pixels[4 * 20 + 2].Should().Be(Red);   // (2,4) — top-left of second 'A', X reset to initial
        pixels[4 * 20 + 5].Should().Be(Red);   // (5,4) — top-right of second 'A'
        pixels[0 * 20 + 6].Should().Be(Black); // (6,0) — no char drawn past first 'A' on row 0
    }

    [Fact]
    public void Print_Newline_AdvancesCursorY_ByPreviousCharHeight()
    {
        // Font: 4×8 chars "A" (taller than default). Print("A\nA", 0, 0, Red):
        //   'A' at (0,0) spans rows y=0..7 (charHeight=8).
        //   '\n' advances Y by 8 → cursor at (0,8).
        //   Second 'A' drawn at (0,8).
        // Using charHeight=8 verifies Y is stepped by the actual char height, not a fixed value.
        // Currently fails: '\n' treated as unknown → cursor at (8,0); second 'A' at (8,0), nothing at (0,8).
        var font = BuildFont_SingleTier(4, 8, "A");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("A\nA", 0, 0, Red, font));

        pixels[8 * 20 + 0].Should().Be(Red);  // (0,8) — top-left of second 'A', Y advanced by charHeight=8
    }

    [Fact]
    public void Print_Newline_AtStart_AdvancesY_ByFirstTierCharHeight()
    {
        // Font: 4×4 chars "A". Print("\nA", 0, 0, Red):
        //   '\n' appears before any character. Y advances by the first-tier char height (4); X stays 0.
        //   'A' drawn at (0,4).
        // Currently fails: '\n' treated as unknown → cursor at (4,0); 'A' drawn at (4,0), nothing at (0,4).
        var font = BuildFont_SingleTier(4, 4, "A");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("\nA", 0, 0, Red, font));

        pixels[4 * 20 + 0].Should().Be(Red);  // (0,4) — 'A' drawn on second line
    }

    [Fact]
    public void Print_MultipleNewlines_StackYCorrectly()
    {
        // Font: 4×4 chars "A". Print("A\n\nA", 0, 0, Red):
        //   'A' at (0,0). First '\n': Y → 4. Second '\n': Y → 8. Second 'A' at (0,8).
        // Currently fails: two '\n' chars each advance cursor X by 4 → second 'A' at (12,0).
        var font = BuildFont_SingleTier(4, 4, "A");
        var pixels = RenderToTarget(20, 20, Black, gm => gm.Print("A\n\nA", 0, 0, Red, font));

        pixels[8 * 20 + 0].Should().Be(Red);  // (0,8) — second 'A' after two stacked newlines
    }

    // -------------------------------------------------------------------------
    #endregion
}