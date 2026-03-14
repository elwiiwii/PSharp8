using FluentAssertions;
using Microsoft.Xna.Framework;
using PSharp8.Graphics;
using Xunit;

namespace PSharp8.Tests.Graphics;

public class PaletteManagerTests
{
    // The first Pico8 palette colour (black) is transparent by default so that
    // colour-index 0 renders as transparent in sprite/map draws.
    private static readonly Color Black = new(0x00, 0x00, 0x00, 255);
    private static readonly Color DarkBlue = new(0x1D, 0x2B, 0x53, 255);
    private static readonly Color Red = new(0xFF, 0x00, 0x4D, 255);

    // -------------------------------------------------------------------------
    #region Construction
    // -------------------------------------------------------------------------

    [Fact]
    public void PaletteMap_HasExactly32Entries()
    {
        var pm = new PaletteManager();

        pm.PaletteMap.Should().HaveCount(32);
    }

    [Fact]
    public void PaletteVersion_StartsAt1_AfterConstruction()
    {
        // Constructor calls ResetPalette(), which increments once.
        var pm = new PaletteManager();

        pm.PaletteVersion.Should().Be(1);
    }

    [Fact]
    public void PaletteMap_DefaultEntry_BlackMapsToTransparent()
    {
        var pm = new PaletteManager();

        // Black (index 0) is special: its default mapped value has A = 0.
        pm.PaletteMap[Black].A.Should().Be(0);
    }

    [Fact]
    public void PaletteMap_DefaultEntries_NonBlackColoursMaintainFullAlpha()
    {
        var pm = new PaletteManager();

        foreach (var (key, value) in pm.PaletteMap)
        {
            if (key == Black) continue;
            value.A.Should().Be(255, because: $"palette entry {key} should default to fully opaque");
        }
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetPalette
    // -------------------------------------------------------------------------

    [Fact]
    public void SetPalette_RemapsTheSpecifiedKey()
    {
        var pm = new PaletteManager();

        pm.SetPalette(DarkBlue, Red);

        pm.PaletteMap[DarkBlue].Should().Be(Red);
    }

    [Fact]
    public void SetPalette_IncrementsVersionByOne()
    {
        var pm = new PaletteManager();
        int before = pm.PaletteVersion;

        pm.SetPalette(DarkBlue, Red);

        pm.PaletteVersion.Should().Be(before + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region SetTransparency
    // -------------------------------------------------------------------------

    [Fact]
    public void SetTransparency_SetsAlphaOnMappedValue()
    {
        var pm = new PaletteManager();

        pm.SetTransparency(DarkBlue, 128);

        pm.PaletteMap[DarkBlue].A.Should().Be(128);
    }

    [Fact]
    public void SetTransparency_ClampsOpacityToZero_WhenNegative()
    {
        var pm = new PaletteManager();

        pm.SetTransparency(DarkBlue, -50);

        pm.PaletteMap[DarkBlue].A.Should().Be(0);
    }

    [Fact]
    public void SetTransparency_ClampsOpacityTo255_WhenAboveMax()
    {
        var pm = new PaletteManager();

        pm.SetTransparency(DarkBlue, 999);

        pm.PaletteMap[DarkBlue].A.Should().Be(255);
    }

    [Fact]
    public void SetTransparency_IncrementsVersionByOne()
    {
        var pm = new PaletteManager();
        int before = pm.PaletteVersion;

        pm.SetTransparency(DarkBlue, 0);

        pm.PaletteVersion.Should().Be(before + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region ResetPalette
    // -------------------------------------------------------------------------

    [Fact]
    public void ResetPalette_RestoresRemappedEntry()
    {
        var pm = new PaletteManager();
        Color original = pm.PaletteMap[DarkBlue];
        pm.SetPalette(DarkBlue, Red);

        pm.ResetPalette();

        pm.PaletteMap[DarkBlue].Should().Be(original);
    }

    [Fact]
    public void ResetPalette_Restores32Entries()
    {
        var pm = new PaletteManager();
        pm.SetPalette(DarkBlue, Red);
        pm.SetPalette(Red, DarkBlue);

        pm.ResetPalette();

        pm.PaletteMap.Should().HaveCount(32);
    }

    [Fact]
    public void ResetPalette_IncrementsVersionByOne()
    {
        var pm = new PaletteManager();
        int before = pm.PaletteVersion;

        pm.ResetPalette();

        pm.PaletteVersion.Should().Be(before + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region ResetTransparency
    // -------------------------------------------------------------------------

    [Fact]
    public void ResetTransparency_SetsAllMappedAlphasTo255()
    {
        var pm = new PaletteManager();
        pm.SetTransparency(DarkBlue, 0);
        pm.SetTransparency(Red, 64);

        pm.ResetTransparency();

        foreach (var value in pm.PaletteMap.Values)
            value.A.Should().Be(255, because: "ResetTransparency should restore full opacity for every entry");
    }

    [Fact]
    public void ResetTransparency_IncrementsVersionByOne()
    {
        var pm = new PaletteManager();
        int before = pm.PaletteVersion;

        pm.ResetTransparency();

        pm.PaletteVersion.Should().Be(before + 1);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Version accumulation
    // -------------------------------------------------------------------------

    [Fact]
    public void MultipleMutations_AccumulateVersionCorrectly()
    {
        var pm = new PaletteManager();
        int baseline = pm.PaletteVersion; // 1 after ctor

        pm.SetPalette(DarkBlue, Red);       // +1
        pm.SetTransparency(Red, 0);         // +1
        pm.ResetTransparency();             // +1
        pm.ResetPalette();                  // +1

        pm.PaletteVersion.Should().Be(baseline + 4);
    }

    #endregion
}
