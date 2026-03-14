using FluentAssertions;
using Microsoft.Xna.Framework;
using PSharp8.Graphics;
using Xunit;

namespace PSharp8.Tests.Graphics;

public class SpriteSnapshotTests
{
    private static readonly Color Blue  = new(0x1D, 0x2B, 0x53, 255);
    private static readonly Color Red   = new(0xFF, 0x00, 0x4D, 255);
    private static readonly Color Green = new(0x00, 0x87, 0x51, 255);

    private static Color[] Solid(int count, Color color)
        => Enumerable.Repeat(color, count).ToArray();

    private static Dictionary<Color, Color> Palette(params (Color key, Color value)[] entries)
        => entries.ToDictionary(e => e.key, e => e.value);

    [Fact]
    public void Equals_ReturnsTrue_ForSamePixelsAndPalette()
    {
        var pixels  = Solid(64, Blue);
        var palette = Palette((Blue, Red));

        var a = new SpriteSnapshot(pixels, 1, 1, palette);
        var b = new SpriteSnapshot(pixels, 1, 1, palette);

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenPixelsDiffer()
    {
        var a = new SpriteSnapshot(Solid(64, Blue), 1, 1, Palette((Blue, Blue)));
        var b = new SpriteSnapshot(Solid(64, Red),  1, 1, Palette((Red, Red)));

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenRelevantPaletteEntryDiffers()
    {
        var pixels = Solid(64, Blue);

        var a = new SpriteSnapshot(pixels, 1, 1, Palette((Blue, Red)));
        var b = new SpriteSnapshot(pixels, 1, 1, Palette((Blue, Green)));

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenIrrelevantPaletteEntryDiffers()
    {
        // pixels are all Blue; remapping Red (not present) must not affect equality
        var pixels = Solid(64, Blue);

        var a = new SpriteSnapshot(pixels, 1, 1, Palette((Blue, Blue), (Red, Red)));
        var b = new SpriteSnapshot(pixels, 1, 1, Palette((Blue, Blue), (Red, Green)));

        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDimensionsDifferForSameSizePixelBuffer()
    {
        // 1×2 and 2×1 both produce 128 pixels but represent different shapes
        var pixels = Solid(128, Blue);

        var a = new SpriteSnapshot(pixels, 1, 2, Palette());
        var b = new SpriteSnapshot(pixels, 2, 1, Palette());

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_IsConsistent_ForEqualSnapshots()
    {
        var pixels  = Solid(64, Blue);
        var palette = Palette((Blue, Red));

        var a = new SpriteSnapshot(pixels, 1, 1, palette);
        var b = new SpriteSnapshot(pixels, 1, 1, palette);

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
