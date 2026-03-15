using FluentAssertions;
using PSharp8.Tests.Infrastructure;
using Xunit;

namespace PSharp8.Tests.Graphics;

[Collection("Graphics")]
public class DrawingPrimitivesTests(GraphicsFixture fixture) : GraphicsTestBase(fixture)
{
    // -------------------------------------------------------------------------
    #region Circ
    // -------------------------------------------------------------------------

    [Fact]
    public void Circ_DrawsCenterPixel_WhenRadiusIsZero()
    {
        // Pico-8 spec: "if r is negative, the circle is not drawn" — r=0 is NOT negative.
        // A circle of radius 0 degenerates to its centre point. The zepto8 reference
        // confirms: the midpoint loop runs once (dx=0,dy=0) and draws the centre cell.
        // Current implementation incorrectly returns early for radius <= 0.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circ(5, 5, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Red); // centre pixel must be painted
    }

    [Fact]
    public void Circ_DrawsNothing_WhenRadiusIsNegative()
    {
        // Pico-8 spec: "if r is negative, the circle is not drawn."
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circ(5, 5, -1, Red));

        // Sample the four cardinal positions a radius-1 circle would occupy.
        pixels[5 * 20 + 6].Should().Be(Black); // right
        pixels[5 * 20 + 4].Should().Be(Black); // left
        pixels[6 * 20 + 5].Should().Be(Black); // bottom
        pixels[4 * 20 + 5].Should().Be(Black); // top
    }

    [Fact]
    public void Circ_DrawsCardinalPoints_WhenRadiusIsOne()
    {
        // r=1, centre (5,5): midpoint loop executes one iteration (dx=1,dy=0),
        // placing pixels at the four cardinal extremes. Centre is not painted.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circ(5, 5, 1, Red));

        pixels[5 * 20 + 6].Should().Be(Red);   // right  (6,5)
        pixels[5 * 20 + 4].Should().Be(Red);   // left   (4,5)
        pixels[6 * 20 + 5].Should().Be(Red);   // bottom (5,6)
        pixels[4 * 20 + 5].Should().Be(Red);   // top    (5,4)
        pixels[5 * 20 + 5].Should().Be(Black); // centre (5,5) — hollow circle
    }

    [Fact]
    public void Circ_DrawsCorrectOutline_WhenRadiusIsTwo()
    {
        // r=2, centre (10,10): two midpoint iterations produce 12 unique pixels.
        // Iteration 1 (dx=2,dy=0): 4 cardinal points.
        // Iteration 2 (dx=2,dy=1): 8 diagonal points.
        var pixels = RenderToTarget(25, 25, Black, gm =>
            gm.Circ(10, 10, 2, Red));

        // Cardinals
        pixels[10 * 25 + 12].Should().Be(Red); // right   (12,10)
        pixels[10 * 25 +  8].Should().Be(Red); // left    (8,10)
        pixels[12 * 25 + 10].Should().Be(Red); // bottom  (10,12)
        pixels[ 8 * 25 + 10].Should().Be(Red); // top     (10,8)

        // Sample diagonals
        pixels[11 * 25 + 12].Should().Be(Red); // (12,11)
        pixels[ 8 * 25 +  9].Should().Be(Red); // (9,8)

        // Centre must stay unpainted
        pixels[10 * 25 + 10].Should().Be(Black); // centre (10,10)
    }

    [Fact]
    public void Circ_AppliesCameraOffset()
    {
        // Camera(3,3) shifts all drawing left/up by 3 cells.
        // Circ(8,8,1) with that offset paints a radius-1 circle centred at cell (5,5).
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(3, 3);
            gm.Circ(8, 8, 1, Red);
        });

        // Cardinal pixels of the offset circle at (5,5)
        pixels[5 * 20 + 6].Should().Be(Red);   // right  (6,5)
        pixels[5 * 20 + 4].Should().Be(Red);   // left   (4,5)
        pixels[6 * 20 + 5].Should().Be(Red);   // bottom (5,6)
        pixels[4 * 20 + 5].Should().Be(Red);   // top    (5,4)

        // Right cardinal of the un-offset circle would sit at (9,8) — must be Black.
        pixels[8 * 20 + 9].Should().Be(Black);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Circfill
    // -------------------------------------------------------------------------

    [Fact]
    public void Circfill_DrawsCenterPixel_WhenRadiusIsZero()
    {
        // Pico-8 spec: "if r is negative, the circle is not drawn" — r=0 is not negative.
        // The midpoint loop runs once (dx=0,dy=0) and draws 4 overlapping scanlines
        // of width 1 at the centre. Current implementation incorrectly returns early
        // for radius <= 0; the fix is radius < 0.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circfill(5, 5, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Red); // centre pixel must be painted
    }

    [Fact]
    public void Circfill_DrawsNothing_WhenRadiusIsNegative()
    {
        // Pico-8 spec: "if r is negative, the circle is not drawn."
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circfill(5, 5, -1, Red));

        pixels[5 * 20 + 6].Should().Be(Black); // right
        pixels[5 * 20 + 4].Should().Be(Black); // left
        pixels[6 * 20 + 5].Should().Be(Black); // below
        pixels[4 * 20 + 5].Should().Be(Black); // above
        pixels[5 * 20 + 5].Should().Be(Black); // centre
    }

    [Fact]
    public void Circfill_FillsExpectedArea_WhenRadiusIsOne()
    {
        // r=1, centre (5,5): one midpoint iteration.
        // Row y=5: scanline x∈[4..6] (width 3). Rows y=4 and y=6: width-1 scanline at x=5.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Circfill(5, 5, 1, Red));

        // Interior cells
        pixels[5 * 20 + 5].Should().Be(Red); // centre  (5,5)
        pixels[5 * 20 + 4].Should().Be(Red); // left    (4,5)
        pixels[5 * 20 + 6].Should().Be(Red); // right   (6,5)
        pixels[4 * 20 + 5].Should().Be(Red); // top     (5,4)
        pixels[6 * 20 + 5].Should().Be(Red); // bottom  (5,6)

        // Outside the disc
        pixels[5 * 20 + 3].Should().Be(Black); // two left of centre
        pixels[5 * 20 + 7].Should().Be(Black); // two right of centre
        pixels[3 * 20 + 5].Should().Be(Black); // two above centre
        pixels[7 * 20 + 5].Should().Be(Black); // two below centre
    }

    [Fact]
    public void Circfill_FillsExpectedArea_WhenRadiusIsTwo()
    {
        // r=2, centre (10,10):
        // Middle row y=10: full scanline x∈[8..12] — (8,10) and (12,10) are Red.
        // Top/bottom cap rows y=8,y=12: width-1 scanline at x=10 — (9,8) is Red, (8,8) Black.
        // Interior centre (10,10) is fully covered.
        var pixels = RenderToTarget(25, 25, Black, gm =>
            gm.Circfill(10, 10, 2, Red));

        pixels[10 * 25 + 10].Should().Be(Red); // centre        (10,10)
        pixels[10 * 25 +  8].Should().Be(Red); // middle left   (8,10)
        pixels[10 * 25 + 12].Should().Be(Red); // middle right  (12,10)
        pixels[ 8 * 25 + 10].Should().Be(Red); // top cap       (10,8)
        pixels[12 * 25 + 10].Should().Be(Red); // bottom cap    (10,12)
        pixels[ 8 * 25 +  9].Should().Be(Red); // top diagonal  (9,8)

        pixels[10 * 25 +  7].Should().Be(Black); // just outside middle left  (7,10)
        pixels[ 8 * 25 +  8].Should().Be(Black); // corner outside top-left   (8,8)
        pixels[13 * 25 + 10].Should().Be(Black); // one below bottom cap      (10,13)
    }

    [Fact]
    public void Circfill_AppliesCameraOffset()
    {
        // Camera(3,3) shifts drawing left/up by 3 cells.
        // Circfill(8,8,1) → effective centre (5,5); filled rows as per r=1 above.
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(3, 3);
            gm.Circfill(8, 8, 1, Red);
        });

        pixels[5 * 20 + 5].Should().Be(Red); // centre  (5,5)
        pixels[5 * 20 + 4].Should().Be(Red); // left    (4,5)
        pixels[5 * 20 + 6].Should().Be(Red); // right   (6,5)

        // Without offset the centre would be at (8,8) — must stay Black.
        pixels[8 * 20 + 8].Should().Be(Black);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Cls
    // -------------------------------------------------------------------------

    [Fact]
    public void Cls_ClearsEntireTargetToGivenColor()
    {
        var pixels = RenderToTarget(10, 10, Black, gm =>
            gm.Cls(Red));

        pixels[0].Should().Be(Red);            // top-left
        pixels[9].Should().Be(Red);            // top-right
        pixels[9 * 10 + 0].Should().Be(Red);  // bottom-left
        pixels[9 * 10 + 9].Should().Be(Red);  // bottom-right
        pixels[5 * 10 + 5].Should().Be(Red);  // centre
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Line
    // -------------------------------------------------------------------------

    [Fact]
    public void Line_DrawsSinglePixel_WhenEndpointsAreEqual()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(5, 5, 5, 5, Red));

        pixels[5 * 20 + 5].Should().Be(Red);
        pixels[5 * 20 + 6].Should().Be(Black); // right neighbour
        pixels[5 * 20 + 4].Should().Be(Black); // left neighbour
        pixels[6 * 20 + 5].Should().Be(Black); // below neighbour
        pixels[4 * 20 + 5].Should().Be(Black); // above neighbour
    }

    [Fact]
    public void Line_DrawsHorizontalLine()
    {
        // (2,5)→(6,5): five pixels along y=5, x∈[2..6].
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(2, 5, 6, 5, Red));

        pixels[5 * 20 + 2].Should().Be(Red);
        pixels[5 * 20 + 3].Should().Be(Red);
        pixels[5 * 20 + 4].Should().Be(Red);
        pixels[5 * 20 + 5].Should().Be(Red);
        pixels[5 * 20 + 6].Should().Be(Red);
        pixels[5 * 20 + 1].Should().Be(Black); // one before start
        pixels[5 * 20 + 7].Should().Be(Black); // one past end
    }

    [Fact]
    public void Line_DrawsVerticalLine()
    {
        // (5,2)→(5,6): five pixels along x=5, y∈[2..6].
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(5, 2, 5, 6, Red));

        pixels[2 * 20 + 5].Should().Be(Red);
        pixels[3 * 20 + 5].Should().Be(Red);
        pixels[4 * 20 + 5].Should().Be(Red);
        pixels[5 * 20 + 5].Should().Be(Red);
        pixels[6 * 20 + 5].Should().Be(Red);
        pixels[1 * 20 + 5].Should().Be(Black); // one before start
        pixels[7 * 20 + 5].Should().Be(Black); // one past end
    }

    [Fact]
    public void Line_DrawsDiagonalLine_WhenSlopeIsOne()
    {
        // (2,2)→(5,5): abs(dx)==abs(dy)==3, horizontal-dominant.
        // Each step increments both x and y: (2,2),(3,3),(4,4),(5,5).
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(2, 2, 5, 5, Red));

        pixels[2 * 20 + 2].Should().Be(Red);
        pixels[3 * 20 + 3].Should().Be(Red);
        pixels[4 * 20 + 4].Should().Be(Red);
        pixels[5 * 20 + 5].Should().Be(Red);
        pixels[2 * 20 + 3].Should().Be(Black); // right of start — off-diagonal
        pixels[3 * 20 + 2].Should().Be(Black); // below start — off-diagonal
    }

    [Fact]
    public void Line_DrawsCorrectPixels_WhenSlopeIsLessThanOne()
    {
        // (0,0)→(6,2): horizontal-dominant (abs(6) > abs(2)).
        // y = round(2 * x/6): (0,0),(1,0),(2,1),(3,1),(4,1),(5,2),(6,2).
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(0, 0, 6, 2, Red));

        pixels[0 * 20 + 0].Should().Be(Red);
        pixels[0 * 20 + 1].Should().Be(Red);
        pixels[1 * 20 + 2].Should().Be(Red);
        pixels[1 * 20 + 3].Should().Be(Red);
        pixels[1 * 20 + 4].Should().Be(Red);
        pixels[2 * 20 + 5].Should().Be(Red);
        pixels[2 * 20 + 6].Should().Be(Red);
        pixels[0 * 20 + 2].Should().Be(Black); // (x=2,y=0) — y is 1 here, not 0
    }

    [Fact]
    public void Line_DrawsCorrectPixels_WhenSlopeIsGreaterThanOne()
    {
        // (0,0)→(2,6): vertical-dominant (abs(6) > abs(2)).
        // x = round(2 * y/6): (0,0),(0,1),(1,2),(1,3),(1,4),(2,5),(2,6).
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(0, 0, 2, 6, Red));

        pixels[0 * 20 + 0].Should().Be(Red);
        pixels[1 * 20 + 0].Should().Be(Red);
        pixels[2 * 20 + 1].Should().Be(Red);
        pixels[3 * 20 + 1].Should().Be(Red);
        pixels[4 * 20 + 1].Should().Be(Red);
        pixels[5 * 20 + 2].Should().Be(Red);
        pixels[6 * 20 + 2].Should().Be(Red);
        pixels[2 * 20 + 0].Should().Be(Black); // (x=0,y=2) — x is 1 here, not 0
    }

    [Fact]
    public void Line_DrawsCorrectPixels_WhenDrawnInReverseDirection()
    {
        // (6,2)→(0,0) must paint the same cells as (0,0)→(6,2).
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Line(6, 2, 0, 0, Red));

        pixels[0 * 20 + 0].Should().Be(Red);
        pixels[0 * 20 + 1].Should().Be(Red);
        pixels[1 * 20 + 2].Should().Be(Red);
        pixels[1 * 20 + 3].Should().Be(Red);
        pixels[1 * 20 + 4].Should().Be(Red);
        pixels[2 * 20 + 5].Should().Be(Red);
        pixels[2 * 20 + 6].Should().Be(Red);
    }

    [Fact]
    public void Line_AppliesCameraOffset()
    {
        // Camera(2,0): Line(7,5,11,5) → screen x∈[5..9] at y=5.
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(2, 0);
            gm.Line(7, 5, 11, 5, Red);
        });

        pixels[5 * 20 + 5].Should().Be(Red);
        pixels[5 * 20 + 9].Should().Be(Red);
        pixels[5 * 20 + 4].Should().Be(Black); // one before camera-adjusted start
        pixels[5 * 20 + 10].Should().Be(Black); // one past camera-adjusted end
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Pset
    // -------------------------------------------------------------------------

    [Fact]
    public void Pset_PaintsExpectedColor_AtGivenCoordinate()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Pset(5, 7, Red));

        pixels[7 * 20 + 5].Should().Be(Red);   // target pixel
        pixels[7 * 20 + 6].Should().Be(Black); // right neighbour
        pixels[8 * 20 + 5].Should().Be(Black); // row below
    }

    [Fact]
    public void Pset_AppliesCameraOffset()
    {
        // Pico-8 pset applies the camera: pixel at world (8,8) with Camera(3,3)
        // should paint screen cell (5,5). Current implementation does not subtract
        // _cameraOffset at all, so it paints (8,8) instead.
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(3, 3);
            gm.Pset(8, 8, Red);
        });

        pixels[5 * 20 + 5].Should().Be(Red);  // camera-adjusted position — currently Black
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Rect
    // -------------------------------------------------------------------------

    [Fact]
    public void Rect_DrawsHollowOutline()
    {
        // Rect(2,2,6,4) → 5-wide × 3-tall outline. Interior row (y=3, x∈[3..5]) stays Black.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rect(2, 2, 6, 4, Red));

        // Corners
        pixels[2 * 20 + 2].Should().Be(Red); // top-left     (2,2)
        pixels[2 * 20 + 6].Should().Be(Red); // top-right    (6,2)
        pixels[4 * 20 + 2].Should().Be(Red); // bottom-left  (2,4)
        pixels[4 * 20 + 6].Should().Be(Red); // bottom-right (6,4)

        // Interior — hollow
        pixels[3 * 20 + 3].Should().Be(Black); // (3,3)
        pixels[3 * 20 + 5].Should().Be(Black); // (5,3)

        // Outside edges
        pixels[2 * 20 + 7].Should().Be(Black); // one right of top-right corner
    }

    [Fact]
    public void Rect_AppliesCameraOffset()
    {
        // Camera(1,0), Rect(3,2,7,2): world rect top edge x∈[3..7] at y=2.
        // Screen top edge should be x∈[2..6] at y=2 — 5 pixels wide.
        // Bug: width = xRight - offsetXLeft + 1 = 7 - 2 + 1 = 6, paints x∈[2..7].
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(1, 0);
            gm.Rect(3, 2, 7, 2, Red);
        });

        pixels[2 * 20 + 2].Should().Be(Red);   // screen left edge  (2,2)
        pixels[2 * 20 + 6].Should().Be(Red);   // screen right edge (6,2)
        pixels[2 * 20 + 7].Should().Be(Black); // one past right edge — currently Red due to bug
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Rectfill
    // -------------------------------------------------------------------------

    [Fact]
    public void Rectfill_FillsEntireArea()
    {
        // Rectfill(2,2,4,4) → 3×3 solid block; all 9 cells Red, neighbours Black.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rectfill(2, 2, 4, 4, Red));

        pixels[2 * 20 + 2].Should().Be(Red); // top-left
        pixels[2 * 20 + 4].Should().Be(Red); // top-right
        pixels[4 * 20 + 2].Should().Be(Red); // bottom-left
        pixels[4 * 20 + 4].Should().Be(Red); // bottom-right
        pixels[3 * 20 + 3].Should().Be(Red); // centre

        pixels[2 * 20 + 5].Should().Be(Black); // one right of top-right
        pixels[5 * 20 + 2].Should().Be(Black); // one below bottom-left
    }

    [Fact]
    public void Rectfill_AppliesCameraOffset()
    {
        // Camera(2,0), Rectfill(4,2,8,4): world rect x∈[4..8], y∈[2..4], 5-wide × 3-tall.
        // Screen rect should be x∈[2..6], y∈[2..4].
        // Bug: width = xRight - offsetXLeft + 1 = 8 - 2 + 1 = 7, paints x∈[2..8].
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(2, 0);
            gm.Rectfill(4, 2, 8, 4, Red);
        });

        pixels[2 * 20 + 2].Should().Be(Red);   // screen left edge  (2,2)
        pixels[2 * 20 + 6].Should().Be(Red);   // screen right edge (6,2)
        pixels[2 * 20 + 7].Should().Be(Black); // one past right edge — currently Red due to bug
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Rrect
    // -------------------------------------------------------------------------

    [Fact]
    public void Rrect_DrawsNothing_WhenWidthIsZero()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrect(5, 5, 0, 6, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Black);
        pixels[5 * 20 + 6].Should().Be(Black);
    }

    [Fact]
    public void Rrect_DrawsNothing_WhenHeightIsZero()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrect(5, 5, 6, 0, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Black);
        pixels[6 * 20 + 5].Should().Be(Black);
    }

    [Fact]
    public void Rrect_DrawsHollowRectangleOutline_WhenRadiusIsZero()
    {
        // r=0: rrect behaves like a plain rect outline.
        // Box at (2,2), 9-wide × 7-tall → right edge x=10, bottom edge y=8.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrect(2, 2, 9, 7, 0, Red));

        // All four corners included (no clipping at r=0)
        pixels[2 * 20 +  2].Should().Be(Red); // top-left     (2,2)
        pixels[2 * 20 + 10].Should().Be(Red); // top-right    (10,2)
        pixels[8 * 20 +  2].Should().Be(Red); // bottom-left  (2,8)
        pixels[8 * 20 + 10].Should().Be(Red); // bottom-right (10,8)

        // Edge midpoints
        pixels[2 * 20 +  6].Should().Be(Red); // top midpoint
        pixels[5 * 20 +  2].Should().Be(Red); // left midpoint
        pixels[5 * 20 + 10].Should().Be(Red); // right midpoint
        pixels[8 * 20 +  6].Should().Be(Red); // bottom midpoint

        // Interior — hollow
        pixels[5 * 20 + 5].Should().Be(Black);
        pixels[4 * 20 + 6].Should().Be(Black);
    }

    [Fact]
    public void Rrect_CutsCorners_WhenRadiusIsTwo()
    {
        // Box at (2,2), 9-wide × 7-tall, r=2.
        // Straight edges: top x∈[4..8] at y=2; left y∈[4..6] at x=2.
        // Arc pixels (midpoint iter1 dx=2,dy=0): top-left arc hits (4,2) and (2,4).
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrect(2, 2, 9, 7, 2, Red));

        // Straight top edge
        pixels[2 * 20 + 4].Should().Be(Red); // (4,2) — start of top straight edge
        pixels[2 * 20 + 6].Should().Be(Red); // (6,2) — midpoint of top edge
        pixels[2 * 20 + 8].Should().Be(Red); // (8,2) — end of top straight edge

        // Arc pixels from top-left corner (arc center at (4,4), r=2)
        pixels[4 * 20 + 2].Should().Be(Red); // (2,4) — leftmost arc pixel
        pixels[3 * 20 + 2].Should().Be(Red); // (2,3) — arc pixel iter2

        // Corners must be clipped (unpainted)
        pixels[2 * 20 +  2].Should().Be(Black); // (2,2)  top-left corner
        pixels[2 * 20 + 10].Should().Be(Black); // (10,2) top-right corner
        pixels[8 * 20 +  2].Should().Be(Black); // (2,8)  bottom-left corner
        pixels[8 * 20 + 10].Should().Be(Black); // (10,8) bottom-right corner

        // Interior — hollow
        pixels[5 * 20 + 5].Should().Be(Black);
    }

    [Fact]
    public void Rrect_ClampsRadius_WhenRadiusExceedsHalfMinDimension()
    {
        // 9×7 box, r=10 — clamped to Math.Min(9,7)/2 = 3.
        // With effective r=3: top straight edge edgeW = 9-6 = 3, at x∈[5..7] y=2.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrect(2, 2, 9, 7, 10, Red));

        pixels[2 * 20 + 5].Should().Be(Red);   // (5,2) — in clamped top straight edge
        pixels[2 * 20 + 2].Should().Be(Black); // (2,2) — corner still clipped with r=3
    }

    [Fact]
    public void Rrect_AppliesCameraOffset()
    {
        // Camera(2,0): Rrect(7,3,9,7,0) → screen top-left (5,3), top-right (13,3).
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(2, 0);
            gm.Rrect(7, 3, 9, 7, 0, Red);
        });

        pixels[3 * 20 +  5].Should().Be(Red);   // (5,3) — camera-adjusted top-left
        pixels[3 * 20 + 13].Should().Be(Red);   // (13,3) — camera-adjusted top-right
        pixels[3 * 20 + 14].Should().Be(Black); // (14,3) — one past right edge
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Rrectfill
    // -------------------------------------------------------------------------

    [Fact]
    public void Rrectfill_DrawsNothing_WhenWidthIsZero()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrectfill(5, 5, 0, 6, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Black);
        pixels[6 * 20 + 5].Should().Be(Black);
    }

    [Fact]
    public void Rrectfill_DrawsNothing_WhenHeightIsZero()
    {
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrectfill(5, 5, 6, 0, 0, Red));

        pixels[5 * 20 + 5].Should().Be(Black);
        pixels[5 * 20 + 6].Should().Be(Black);
    }

    [Fact]
    public void Rrectfill_FillsEntireRectangle_WhenRadiusIsZero()
    {
        // r=0 → middle band covers full area; corner loop still runs but only
        // repaints already-covered rows. All cells x∈[2..10], y∈[2..8] must be Red.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrectfill(2, 2, 9, 7, 0, Red));

        // All four corners included — no clipping at r=0
        pixels[2 * 20 +  2].Should().Be(Red); // top-left     (2,2)
        pixels[2 * 20 + 10].Should().Be(Red); // top-right    (10,2)
        pixels[8 * 20 +  2].Should().Be(Red); // bottom-left  (2,8)
        pixels[8 * 20 + 10].Should().Be(Red); // bottom-right (10,8)

        // Interior
        pixels[5 * 20 + 6].Should().Be(Red);

        // Just outside
        pixels[2 * 20 + 11].Should().Be(Black); // one right of top-right
        pixels[9 * 20 +  2].Should().Be(Black); // one below bottom-left
    }

    [Fact]
    public void Rrectfill_FillsDiscWithClippedCorners_WhenRadiusIsTwo()
    {
        // Box at (2,2), 9-wide × 7-tall, r=2. tlx=4, trx=8, tly=4, bly=6.
        // Middle band y∈[4..6] x∈[2..10] fully Red.
        // Arc row y=2: iter1 draws x∈[4..8], iter2 draws x∈[3..9] → union x∈[3..9].
        // Arc row y=3: full-width scanline x∈[2..10].
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrectfill(2, 2, 9, 7, 2, Red));

        pixels[5 * 20 +  5].Should().Be(Red); // centre         (5,5)
        pixels[4 * 20 +  2].Should().Be(Red); // middle-band left edge (2,4)
        pixels[4 * 20 + 10].Should().Be(Red); // middle-band right edge (10,4)
        pixels[3 * 20 +  2].Should().Be(Red); // full-width arc row (2,3)
        pixels[2 * 20 +  3].Should().Be(Red); // narrow arc row start (3,2)
        pixels[2 * 20 +  9].Should().Be(Red); // narrow arc row end (9,2)

        // Corners clipped
        pixels[2 * 20 +  2].Should().Be(Black); // (2,2)  top-left corner
        pixels[2 * 20 + 10].Should().Be(Black); // (10,2) top-right corner
        pixels[8 * 20 +  2].Should().Be(Black); // (2,8)  bottom-left corner
        pixels[8 * 20 + 10].Should().Be(Black); // (10,8) bottom-right corner
    }

    [Fact]
    public void Rrectfill_ClampsRadius_WhenRadiusExceedsHalfMinDimension()
    {
        // 9×7 box, r=10 — clamped to Math.Min(9,7)/2 = 3.
        // Middle band: edgeH = 7-6 = 1, y = 2+3 = 5, x∈[2..10] → (5,5) Red.
        // Corners (2,2) are clipped even with clamped r=3.
        var pixels = RenderToTarget(20, 20, Black, gm =>
            gm.Rrectfill(2, 2, 9, 7, 10, Red));

        pixels[5 * 20 + 5].Should().Be(Red);   // interior midpoint still covered
        pixels[2 * 20 + 2].Should().Be(Black); // (2,2) — corner clipped with r=3
    }

    [Fact]
    public void Rrectfill_AppliesCameraOffset()
    {
        // Camera(2,0): Rrectfill(7,3,9,7,0) → solid block screen x∈[5..13], y∈[3..9].
        var pixels = RenderToTarget(20, 20, Black, gm =>
        {
            gm.Camera(2, 0);
            gm.Rrectfill(7, 3, 9, 7, 0, Red);
        });

        pixels[3 * 20 +  5].Should().Be(Red);   // (5,3)  — camera-adjusted top-left
        pixels[9 * 20 + 13].Should().Be(Red);   // (13,9) — camera-adjusted bottom-right
        pixels[3 * 20 +  4].Should().Be(Black); // (4,3)  — one left of left edge
        pixels[3 * 20 + 14].Should().Be(Black); // (14,3) — one past right edge
    }

    // -------------------------------------------------------------------------
    #endregion
}