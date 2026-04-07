using FluentAssertions;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Fna")]
public class LowLevelDrawingTests(FnaFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
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

    // --- Viewport scaling (handled by GameOrchestrator, not GraphicsManager) ---

    [Fact]
    public void DrawScaledPixel_DrawsAtNativeCoords_WhenViewportLargerThanCellGrid()
    {
        // GraphicsManager no longer applies viewport-to-cell scaling.
        // Cell (5,5) maps to pixel (5,5) regardless of viewport size vs cell resolution.
        var pixels = RenderToTarget(400, 400, Black,
            gm => gm.DrawScaledPixel(5, 5, Red),
            cellResolution: (100, 100));

        pixels[5 * 400 + 5].Should().Be(Red);    // native coordinate (5,5)
        pixels[20 * 400 + 20].Should().Be(Black); // scale-4 position — not painted
    }

    [Fact]
    public void DrawScaledPixel_BlockUsesNativeScaleParams_RegardlessOfCellResolution()
    {
        // scaleX=3, scaleY=2 always expands to a 3×2 native pixel block.
        // Viewport-to-cell scaling is not multiplied in.
        var pixels = RenderToTarget(400, 400, Black,
            gm => gm.DrawScaledPixel(0, 0, Red, scaleX: 3, scaleY: 2),
            cellResolution: (100, 100));

        pixels[0 * 400 + 0].Should().Be(Red);    // top-left
        pixels[0 * 400 + 2].Should().Be(Red);    // top-right of 3-wide block
        pixels[1 * 400 + 0].Should().Be(Red);    // bottom-left of 2-tall block
        pixels[0 * 400 + 3].Should().Be(Black);  // one pixel past 3-wide block
        pixels[2 * 400 + 0].Should().Be(Black);  // one row past 2-tall block
    }

    // -------------------------------------------------------------------------
    #endregion
}