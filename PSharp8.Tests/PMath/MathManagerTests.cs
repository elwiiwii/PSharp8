using FixMath;
using FluentAssertions;
using PSharp8.PMath;
using Xunit;

namespace PSharp8.Tests.PMath;

public sealed class MathManagerTests
{
    private readonly MathManager _sut = new();

    // --------------------------------------------------------------------------
    #region Arithmetic
    // --------------------------------------------------------------------------

    [Fact]
    public void Abs_ReturnsPositive_WhenNegativeInput()
    {
        var result = _sut.Abs(F32.FromDouble(-3.5));
        result.Double.Should().Be(3.5);
    }

    [Fact]
    public void Abs_ReturnsPositive_WhenPositiveInput()
    {
        var result = _sut.Abs(F32.FromDouble(3.5));
        result.Double.Should().Be(3.5);
    }

    [Fact]
    public void Abs_ReturnsZero_WhenZero()
    {
        var result = _sut.Abs(F32.Zero);
        result.Double.Should().Be(0.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Floor
    // --------------------------------------------------------------------------

    [Fact]
    public void Flr_RoundsDown_WhenPositive()
    {
        var result = _sut.Flr(F32.FromDouble(3.9));
        result.Double.Should().Be(3.0);
    }

    [Fact]
    public void Flr_RoundsDown_WhenNegative()
    {
        var result = _sut.Flr(F32.FromDouble(-3.1));
        result.Double.Should().Be(-4.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Ceil
    // --------------------------------------------------------------------------

    [Fact]
    public void Ceil_RoundsUp_WhenPositive()
    {
        var result = _sut.Ceil(F32.FromDouble(3.1));
        result.Double.Should().Be(4.0);
    }

    [Fact]
    public void Ceil_RoundsUp_WhenNegative()
    {
        var result = _sut.Ceil(F32.FromDouble(-3.9));
        result.Double.Should().Be(-3.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Max and Min
    // --------------------------------------------------------------------------

    [Fact]
    public void Max_ReturnsLarger()
    {
        var result = _sut.Max(F32.FromDouble(2), F32.FromDouble(5));
        result.Double.Should().Be(5.0);
    }

    [Fact]
    public void Max_ReturnsFirst_WhenEqual()
    {
        var result = _sut.Max(F32.FromDouble(5), F32.FromDouble(5));
        result.Double.Should().Be(5.0);
    }

    [Fact]
    public void Min_ReturnsSmaller()
    {
        var result = _sut.Min(F32.FromDouble(2), F32.FromDouble(5));
        result.Double.Should().Be(2.0);
    }

    [Fact]
    public void Min_ReturnsFirst_WhenEqual()
    {
        var result = _sut.Min(F32.FromDouble(2), F32.FromDouble(2));
        result.Double.Should().Be(2.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Mid
    // --------------------------------------------------------------------------

    [Fact]
    public void Mid_ReturnsMiddle_WhenDistinctValues()
    {
        var result = _sut.Mid(F32.FromDouble(3), F32.FromDouble(1), F32.FromDouble(5));
        result.Double.Should().Be(3.0);
    }

    [Fact]
    public void Mid_ReturnsValue_WhenAllEqual()
    {
        var result = _sut.Mid(F32.FromDouble(5), F32.FromDouble(5), F32.FromDouble(5));
        result.Double.Should().Be(5.0);
    }

    [Fact]
    public void Mid_ReturnsMiddle_WhenUnsortedOrder()
    {
        // median is order-independent: Mid(5, 1, 3) → 3
        var result = _sut.Mid(F32.FromDouble(5), F32.FromDouble(1), F32.FromDouble(3));
        result.Double.Should().Be(3.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Sgn
    // --------------------------------------------------------------------------

    [Fact]
    public void Sgn_ReturnsOne_WhenPositive()
    {
        var result = _sut.Sgn(F32.FromDouble(3.5));
        result.Double.Should().Be(1.0);
    }

    [Fact]
    public void Sgn_ReturnsNegativeOne_WhenNegative()
    {
        var result = _sut.Sgn(F32.FromDouble(-3.5));
        result.Double.Should().Be(-1.0);
    }

    [Fact]
    public void Sgn_ReturnsZero_WhenZero()
    {
        var result = _sut.Sgn(F32.Zero);
        result.Double.Should().Be(0.0);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Mod
    // --------------------------------------------------------------------------

    [Theory]
    [InlineData(-1.0, 4.0, 3.0)]   // negative dividend → positive result (Lua/pico-8 semantics)
    [InlineData(7.0, 4.0, 3.0)]    // standard positive case
    [InlineData(4.0, 4.0, 0.0)]    // exact multiple → 0
    [InlineData(0.25, 1.0, 0.25)]  // fractional inputs
    public void Mod_ReturnsCorrectRemainder_LuaSemantics(double a, double b, double expected)
    {
        var result = _sut.Mod(F32.FromDouble(a), F32.FromDouble(b));
        result.Double.Should().BeApproximately(expected, 0.001);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Sin
    // --------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0.0)]      // zero input
    [InlineData(0.25, -1.0)]    // quarter turn → -1 (pico-8 inverted convention)
    [InlineData(0.5, 0.0)]      // half turn → 0
    [InlineData(0.75, 1.0)]     // three-quarter turn → 1
    [InlineData(1.0, 0.0)]      // full turn wraps to 0
    [InlineData(1.25, -1.0)]    // input > 1 wraps (same result as 0.25)
    [InlineData(-0.25, 1.0)]    // negative input wraps (same result as 0.75)
    public void Sin_ReturnsExpectedValue_GivenTurnInput(double turns, double expected)
    {
        var result = _sut.Sin(F32.FromDouble(turns));
        result.Double.Should().BeApproximately(expected, 0.001);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Cos
    // --------------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 1.0)]      // zero input → 1
    [InlineData(0.25, 0.0)]     // quarter turn → 0
    [InlineData(0.5, -1.0)]     // half turn → -1
    [InlineData(0.75, 0.0)]     // three-quarter turn → 0
    [InlineData(1.0, 1.0)]      // full turn wraps back to 1
    [InlineData(-0.25, 0.0)]    // negative input wraps (same result as 0.75)
    public void Cos_ReturnsExpectedValue_GivenTurnInput(double turns, double expected)
    {
        var result = _sut.Cos(F32.FromDouble(turns));
        result.Double.Should().BeApproximately(expected, 0.001);
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Rnd
    // --------------------------------------------------------------------------

    [Fact]
    public void Rnd_ReturnsValueInRange_GivenMaxOne()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.Rnd(F32.One, null);
            result.Double.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThan(1.0);
        }
    }

    [Fact]
    public void Rnd_ReturnsValueInRange_GivenMaxFive()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.Rnd(F32.FromDouble(5), null);
            result.Double.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThan(5.0);
        }
    }

    // --------------------------------------------------------------------------
    #endregion
    // --------------------------------------------------------------------------
    #region Srand
    // --------------------------------------------------------------------------

    [Fact]
    public void Srand_ProducesDeterministicSequence_GivenSameSeed()
    {
        _sut.Srand(F32.FromDouble(42), (Random?)null);
        var first = Enumerable.Range(0, 5)
            .Select(_ => _sut.Rnd(F32.One, null).Double)
            .ToList();

        _sut.Srand(F32.FromDouble(42), (Random?)null);
        var second = Enumerable.Range(0, 5)
            .Select(_ => _sut.Rnd(F32.One, null).Double)
            .ToList();

        first.Should().Equal(second);
    }

    // --------------------------------------------------------------------------
    #endregion
}
