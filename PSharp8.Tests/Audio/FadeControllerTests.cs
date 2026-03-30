using FluentAssertions;
using PSharp8.Audio;
using Xunit;

namespace PSharp8.Tests.Audio;

public class FadeControllerTests
{
    // -------------------------------------------------------------------------
    #region Defaults
    // -------------------------------------------------------------------------

    [Fact]
    public void IsFading_ReturnsFalse_AfterConstruction()
    {
        var sut = new FadeController();

        sut.IsFading.Should().BeFalse();
    }

    [Fact]
    public void IsFadingOut_ReturnsFalse_AfterConstruction()
    {
        var sut = new FadeController();

        sut.IsFadingOut.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region BeginFade
    // -------------------------------------------------------------------------

    [Fact]
    public void BeginFade_SetsIsFadingTrue()
    {
        var sut = new FadeController();

        sut.BeginFade(1000, 0f, 1f, fadingOut: false);

        sut.IsFading.Should().BeTrue();
    }

    [Fact]
    public void BeginFade_SetsIsFadingOutTrue_WhenFadingOut()
    {
        var sut = new FadeController();

        sut.BeginFade(1000, 1f, 0f, fadingOut: true);

        sut.IsFadingOut.Should().BeTrue();
    }

    [Fact]
    public void BeginFade_ResetsElapsedMs()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 0f, 1f, fadingOut: false);
        sut.Update(500); // advance 500ms

        sut.BeginFade(2000, 0.5f, 1f, fadingOut: false); // new fade

        sut.FadeElapsedMs.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Update
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_ReturnsNull_WhenNotFading()
    {
        var sut = new FadeController();

        var result = sut.Update(100);

        result.Should().BeNull();
    }

    [Fact]
    public void Update_ReturnsInterpolatedVolume_DuringFadeIn()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 0f, 1f, fadingOut: false);

        var result = sut.Update(500); // halfway

        result.Should().BeApproximately(0.5f, 0.01f);
    }

    [Fact]
    public void Update_ReturnsInterpolatedVolume_DuringFadeOut()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 1f, 0f, fadingOut: true);

        var result = sut.Update(500); // halfway

        result.Should().BeApproximately(0.5f, 0.01f);
    }

    [Fact]
    public void Update_ReturnsNull_WhenFadeCompletes()
    {
        var sut = new FadeController();
        sut.BeginFade(100, 0f, 1f, fadingOut: false);

        var result = sut.Update(100); // exactly done

        result.Should().BeNull("caller should use Complete() to finalize");
        sut.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Update_ReturnsNull_WhenElapsedExceedsDuration()
    {
        var sut = new FadeController();
        sut.BeginFade(100, 0f, 1f, fadingOut: false);

        var result = sut.Update(999); // overshoot

        result.Should().BeNull();
        sut.IsComplete.Should().BeTrue();
    }

    [Fact]
    public void Update_InterpolatesArbitraryRange()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 1f, 0.3f, fadingOut: false); // fade from 1.0 to 0.3

        var result = sut.Update(500); // halfway → 0.65

        result.Should().BeApproximately(0.65f, 0.01f);
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Complete
    // -------------------------------------------------------------------------

    [Fact]
    public void Complete_ReturnsTargetVolume_WhenFadeActive()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 0f, 1f, fadingOut: false);
        sut.Update(500);

        var result = sut.Complete();

        result.Should().Be(1f);
    }

    [Fact]
    public void Complete_ResetsFadeState()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 0f, 1f, fadingOut: false);
        sut.Update(500);

        sut.Complete();

        sut.IsFading.Should().BeFalse();
        sut.IsFadingOut.Should().BeFalse();
    }

    [Fact]
    public void Complete_ReturnsNull_WhenNotFading()
    {
        var sut = new FadeController();

        var result = sut.Complete();

        result.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    #endregion
    #region Reset
    // -------------------------------------------------------------------------

    [Fact]
    public void Reset_ClearsAllFadeState()
    {
        var sut = new FadeController();
        sut.BeginFade(1000, 0.5f, 1f, fadingOut: true);
        sut.Update(300);

        sut.Reset();

        sut.IsFading.Should().BeFalse();
        sut.IsFadingOut.Should().BeFalse();
        sut.FadeElapsedMs.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    #endregion
}
