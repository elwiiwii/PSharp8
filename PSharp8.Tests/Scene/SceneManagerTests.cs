using FluentAssertions;
using Moq;
using PSharp8.Audio;
using PSharp8.Input;
using PSharp8.Scene;
using Xunit;

namespace PSharp8.Tests.Scene;

public class SceneManagerTests
{
    // --- Constructor ---

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenInputManagerIsNull()
    {
        var act = () => new SceneManager(inputManager: null!);

        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("inputManager");
    }

    // --- Accumulator / callback timing ---

    [Fact]
    public void InternalUpdate_FiresCallback_WhenElapsedReachesInterval()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100));

        callCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_DoesNotFireCallback_WhenElapsedBelowInterval()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(99));

        callCount.Should().Be(0);
    }

    [Fact]
    public void InternalUpdate_AccumulatesAcrossMultipleTicks()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // accumulated: 60ms — no fire
        sut.InternalUpdate(TimeSpan.FromMilliseconds(60)); // accumulated: 120ms — fires once

        callCount.Should().Be(1);
    }

    [Fact]
    public void InternalUpdate_FiresCallbackTwice_WhenElapsedCoversMultipleIntervals()
    {
        var callCount = 0;
        var sut = BuildWithScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10));

        sut.InternalUpdate(TimeSpan.FromMilliseconds(250)); // 200ms consumed, 50ms remainder

        callCount.Should().Be(2);
    }

    // --- Scene transition ---

    [Fact]
    public void InternalUpdate_DoesNotFireCallback_BeforeSceneIsApplied()
    {
        var callCount = 0;
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);

        // Queue a scene but never call InternalUpdate — scene never becomes active
        sut.ScheduleScene(() => new TestScene(setup => setup.RegisterUpdate(() => callCount++, fps: 10)));

        callCount.Should().Be(0);
    }

    // --- Multiple callbacks at different rates ---

    [Fact]
    public void InternalUpdate_TwoCallbacksAtDifferentFps_FireIndependently()
    {
        var aCount = 0;
        var bCount = 0;
        var sut = BuildWithScene(setup =>
        {
            setup.RegisterUpdate(() => aCount++, fps: 10); // fires every 100ms
            setup.RegisterUpdate(() => bCount++, fps: 5);  // fires every 200ms
        });

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A fires (total: A=1, B=0)
        aCount.Should().Be(1);
        bCount.Should().Be(0);

        sut.InternalUpdate(TimeSpan.FromMilliseconds(100)); // A fires again, B fires once (total: A=2, B=1)
        aCount.Should().Be(2);
        bCount.Should().Be(1);
    }

    // --- Helpers ---

    /// <summary>
    /// Creates a <see cref="SceneManager"/>, schedules a <see cref="TestScene"/> configured
    /// by <paramref name="configure"/>, then applies the transition with a zero-elapsed tick
    /// so callbacks are registered and ready for the actual test ticks.
    /// </summary>
    private static SceneManager BuildWithScene(Action<ISceneSetup> configure)
    {
        var mock = new Mock<IInputManager>();
        var sut = new SceneManager(mock.Object);
        sut.ScheduleScene(() => new TestScene(configure));
        sut.InternalUpdate(TimeSpan.Zero); // applies the queued transition
        return sut;
    }

    // --- Test double ---

    private sealed class TestScene : IScene
    {
        private readonly Action<ISceneSetup> _configure;

        public TestScene(Action<ISceneSetup> configure) => _configure = configure;

        public string? Name => null;
        public void Init(ISceneSetup setup) => _configure(setup);
        public string? SpritesPath => null;
        public string? MapPath => null;
        public string? FlagDataPath => null;
        public IReadOnlyList<Soundtrack> Music => [];
        public IReadOnlyList<SfxPack> Sfx => [];
    }
}
